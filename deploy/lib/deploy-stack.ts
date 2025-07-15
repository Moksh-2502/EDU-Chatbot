import * as cdk from 'aws-cdk-lib';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import * as ecs from 'aws-cdk-lib/aws-ecs';
import * as ecsp from 'aws-cdk-lib/aws-ecs-patterns';
import * as secretsmanager from 'aws-cdk-lib/aws-secretsmanager';
import * as acm from 'aws-cdk-lib/aws-certificatemanager';
import * as r53 from 'aws-cdk-lib/aws-route53';
import * as s3 from 'aws-cdk-lib/aws-s3';
import * as dynamodb from 'aws-cdk-lib/aws-dynamodb';
import * as cloudfront from 'aws-cdk-lib/aws-cloudfront';
import * as cloudfrontOrigins from 'aws-cdk-lib/aws-cloudfront-origins';
import * as cognito from 'aws-cdk-lib/aws-cognito';
import type { Construct } from 'constructs';

interface DeployStackProps extends cdk.StackProps {
  vpcId: string;
  secretId: string;
  certificateArn: string;
  domainName: string;
  subdomainName: string;
  defaultGameSkeleton?: string;
  bucketName?: string;
}

export class DeployStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props: DeployStackProps) {
    super(scope, id, props);

    const vpc = this.getVpc(props.vpcId);
    const secret = this.getSecret(props.secretId);
    const gameAssetsBucket = this.createGameAssetsBucket(props.bucketName);
    const table = this.createDynamoDbTable();
    const cognito = this.createCognitoUserPool(props);
    const cloudfrontDistribution =
      this.createCloudFrontDistribution(gameAssetsBucket);

    const fargateService = this.createFargateService(
      props,
      vpc,
      secret,
      gameAssetsBucket,
      table,
      cloudfrontDistribution,
      cognito,
    );

    this.configureHealthCheck(fargateService);
  }

  private getVpc(vpcId: string): ec2.IVpc {
    return ec2.Vpc.fromLookup(this, 'Vpc', {
      vpcId,
    });
  }

  private getSecret(secretId: string): secretsmanager.ISecret {
    return secretsmanager.Secret.fromSecretNameV2(this, 'Secret', secretId);
  }

  private createGameAssetsBucket(bucketName?: string): s3.IBucket {
    if (bucketName) {
      // Import existing bucket - configuration must be applied manually or via separate deployment
      const existingBucket = s3.Bucket.fromBucketName(this, 'GameAssetsBucket', bucketName);

      // Note: For existing buckets, you may need to manually apply the following configuration
      // via AWS Console or CLI if not already configured:
      // 1. Enable versioning
      // 2. Enable S3 managed encryption
      // 3. Block all public access
      // 4. Configure CORS (allow GET/HEAD from any origin)
      // 5. Set lifecycle rules to delete PR builds after 14 days
      //    - generated-games/PR/* (14 days)
      //    - skeletons/PR/* (14 days)

      return existingBucket;
    }

    // Create new bucket with full configuration
    const bucket = new s3.Bucket(this, 'GameAssetsBucket', {
      versioned: true,
      encryption: s3.BucketEncryption.S3_MANAGED,
      blockPublicAccess: s3.BlockPublicAccess.BLOCK_ALL,
      removalPolicy: cdk.RemovalPolicy.RETAIN,
      autoDeleteObjects: false,
      cors: [
        {
          allowedHeaders: ['*'],
          allowedMethods: [s3.HttpMethods.GET, s3.HttpMethods.HEAD],
          allowedOrigins: ['*'],
          exposedHeaders: [
            'ETag',
            'Content-Length',
            'Content-Type',
            'Last-Modified',
          ],
          maxAge: 86400,
        },
      ],
      lifecycleRules: [
        {
          id: 'DeletePRBuildsAfter2Weeks',
          enabled: true,
          prefix: 'generated-games/PR',
          expiration: cdk.Duration.days(14),
          noncurrentVersionExpiration: cdk.Duration.days(14),
        },
        {
          id: 'DeleteSkeletonPRBuildsAfter2Weeks',
          enabled: true,
          prefix: 'skeletons/PR',
          expiration: cdk.Duration.days(14),
          noncurrentVersionExpiration: cdk.Duration.days(14),
        },
      ],
    });

    return bucket;
  }

  private createDynamoDbTable(): dynamodb.Table {
    const table = new dynamodb.Table(this, 'ChatTable', {
      partitionKey: { name: 'PK', type: dynamodb.AttributeType.STRING },
      sortKey: { name: 'SK', type: dynamodb.AttributeType.STRING },
      billingMode: dynamodb.BillingMode.PAY_PER_REQUEST,
      removalPolicy: cdk.RemovalPolicy.RETAIN,
    });

    table.addGlobalSecondaryIndex({
      indexName: 'GSI1',
      partitionKey: { name: 'GSI1PK', type: dynamodb.AttributeType.STRING },
      sortKey: { name: 'GSI1SK', type: dynamodb.AttributeType.STRING },
      projectionType: dynamodb.ProjectionType.ALL,
    });

    return table;
  }

  private createCognitoUserPool(props: DeployStackProps): {
    userPool: cognito.UserPool;
    userPoolClient: cognito.UserPoolClient;
    userPoolDomain: cognito.UserPoolDomain;
  } {
    // Create User Pool
    const userPool = new cognito.UserPool(this, 'MultiAuthUserPool', {
      userPoolName: 'ai-edu-chatbot-multi-auth',
      selfSignUpEnabled: true,
      signInAliases: {
        email: true,
        username: true
      },
      autoVerify: {
        email: true
      },
      standardAttributes: {
        email: { required: true, mutable: true },
        fullname: { required: false, mutable: true },
        givenName: { required: false, mutable: true },
        familyName: { required: false, mutable: true },
      },

      passwordPolicy: {
        minLength: 8,
        requireLowercase: true,
        requireUppercase: true,
        requireDigits: true,
        requireSymbols: false,
      },
      accountRecovery: cognito.AccountRecovery.EMAIL_ONLY,
      removalPolicy: cdk.RemovalPolicy.RETAIN,
    });

    // Create User Pool Domain
    const userPoolDomain = new cognito.UserPoolDomain(this, 'UserPoolDomain', {
      userPool,
      cognitoDomain: {
        domainPrefix: `ai-edu-chatbot-${cdk.Stack.of(this).account}-${cdk.Stack.of(this).region}`,
      },
    });

    // Create Google Identity Provider
    const googleProvider = new cognito.UserPoolIdentityProviderGoogle(this, 'GoogleProvider', {
      userPool,
      clientId: secretsmanager.Secret.fromSecretNameV2(this, 'GoogleClientIdSecret', props.secretId)
        .secretValueFromJson('GOOGLE_CLIENT_ID')
        .unsafeUnwrap(),
      clientSecretValue: secretsmanager.Secret.fromSecretNameV2(this, 'GoogleClientSecretSecret', props.secretId)
        .secretValueFromJson('GOOGLE_CLIENT_SECRET'),
      scopes: ['openid', 'email', 'profile'],
      attributeMapping: {
        email: cognito.ProviderAttribute.GOOGLE_EMAIL,
        givenName: cognito.ProviderAttribute.GOOGLE_GIVEN_NAME,
        familyName: cognito.ProviderAttribute.GOOGLE_FAMILY_NAME,
        fullname: cognito.ProviderAttribute.GOOGLE_NAME,
      },
    });

    // Create User Pool Client
    const userPoolClient = new cognito.UserPoolClient(this, 'MultiAuthClient', {
      userPool,
      userPoolClientName: 'ai-edu-chatbot-client',
      generateSecret: true,
      authFlows: {
        adminUserPassword: true,
        userPassword: true,
        userSrp: true,
        custom: true,
      },
      oAuth: {
        flows: {
          authorizationCodeGrant: true,
        },
        scopes: [
          cognito.OAuthScope.OPENID,
          cognito.OAuthScope.EMAIL,
          cognito.OAuthScope.PROFILE,
        ],
        callbackUrls: [
          `https://${props.subdomainName}.${props.domainName}/api/auth/callback/cognito`,
          'http://localhost:3000/api/auth/callback/cognito',
        ],
        logoutUrls: [
          `https://${props.subdomainName}.${props.domainName}`,
          'http://localhost:3000',
        ],
      },
      supportedIdentityProviders: [
        cognito.UserPoolClientIdentityProvider.GOOGLE,
        cognito.UserPoolClientIdentityProvider.COGNITO,
      ],
      refreshTokenValidity: cdk.Duration.days(30),
      accessTokenValidity: cdk.Duration.hours(1),
      idTokenValidity: cdk.Duration.hours(1),
      preventUserExistenceErrors: true,
    });

    // Ensure Google provider is created before the client
    userPoolClient.node.addDependency(googleProvider);

    // Output important values
    new cdk.CfnOutput(this, 'CognitoUserPoolId', {
      value: userPool.userPoolId,
      description: 'Cognito User Pool ID',
    });

    new cdk.CfnOutput(this, 'CognitoUserPoolClientId', {
      value: userPoolClient.userPoolClientId,
      description: 'Cognito User Pool Client ID',
    });

    new cdk.CfnOutput(this, 'CognitoUserPoolDomain', {
      value: userPoolDomain.domainName,
      description: 'Cognito User Pool Domain',
    });

    new cdk.CfnOutput(this, 'CognitoIssuerUrl', {
      value: `https://cognito-idp.${cdk.Stack.of(this).region}.amazonaws.com/${userPool.userPoolId}`,
      description: 'Cognito OIDC Issuer URL',
    });

    return { userPool, userPoolClient, userPoolDomain };
  }

  private createCloudFrontDistribution(
    gameAssetsBucket: s3.IBucket,
  ): cloudfront.Distribution {
    const distribution = new cloudfront.Distribution(
      this,
      'GameAssetsDistribution',
      {
        defaultBehavior: {
          origin:
            cloudfrontOrigins.S3BucketOrigin.withOriginAccessControl(
              gameAssetsBucket,
            ),
          originRequestPolicy: cloudfront.OriginRequestPolicy.CORS_S3_ORIGIN,
          viewerProtocolPolicy:
            cloudfront.ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
          cachePolicy: cloudfront.CachePolicy.CACHING_OPTIMIZED,
          allowedMethods: cloudfront.AllowedMethods.ALLOW_GET_HEAD_OPTIONS,
          compress: true,
          responseHeadersPolicy:
            cloudfront.ResponseHeadersPolicy
              .CORS_ALLOW_ALL_ORIGINS_WITH_PREFLIGHT,
        },
        priceClass: cloudfront.PriceClass.PRICE_CLASS_100,
        comment: 'CDN for game assets to improve loading performance',
      },
    );

    new cdk.CfnOutput(this, 'CloudFrontDistributionDomainName', {
      value: distribution.distributionDomainName,
    });

    return distribution;
  }

  private createFargateService(
    props: DeployStackProps,
    vpc: ec2.IVpc,
    appSecret: secretsmanager.ISecret,
    gameAssetsBucket: s3.IBucket,
    table: dynamodb.Table,
    cloudfrontDistribution: cloudfront.Distribution,
    cognito: {
      userPool: cognito.UserPool;
      userPoolClient: cognito.UserPoolClient;
      userPoolDomain: cognito.UserPoolDomain;
    },
  ): ecsp.ApplicationLoadBalancedFargateService {
    const domainProps = props.domainName.endsWith('devfactory.com')
      ? {
        domainName: `${props.subdomainName}.${props.domainName}`,
        domainZone: r53.HostedZone.fromLookup(this, 'DomainZone', {
          domainName: props.domainName,
        }),
      }
      : {};

    const fargateService = new ecsp.ApplicationLoadBalancedFargateService(
      this,
      'NextJsService',
      {
        ...domainProps,
        vpc,
        cpu: 1024,
        memoryLimitMiB: 2048,
        ephemeralStorageGiB: 40,
        desiredCount: 1,
        taskImageOptions: {
          image: ecs.ContainerImage.fromAsset('../', {
            buildArgs: {
              // Analytics Environment Variables (matches Unity build parameters)
              NEXT_PUBLIC_SENTRY_ENVIRONMENT: process.env.NEXT_PUBLIC_SENTRY_ENVIRONMENT || 'dev',
              NEXT_PUBLIC_BUILD_ID: process.env.NEXT_PUBLIC_BUILD_ID || 'dev',
              NEXT_PUBLIC_SENTRY_DSN: process.env.NEXT_PUBLIC_SENTRY_DSN || '',
              SENTRY_AUTH_TOKEN: process.env.SENTRY_AUTH_TOKEN || '',
              NEXT_PUBLIC_MIXPANEL_TOKEN: process.env.NEXT_PUBLIC_MIXPANEL_TOKEN || '',
              NEXT_PUBLIC_COGNITO_CLIENT_ID: process.env.NEXT_PUBLIC_COGNITO_CLIENT_ID || '',
              NEXT_PUBLIC_COGNITO_DOMAIN: process.env.NEXT_PUBLIC_COGNITO_DOMAIN || '',
            },
          }),
          containerPort: 3000,
          environment: {
            AWS_REGION: cdk.Stack.of(this).region,
            AI_PROVIDER: 'openai',
            NEXT_PUBLIC_APP_URL: `https://${props.subdomainName}.${props.domainName}`,
            NEXTAUTH_URL: `https://${props.subdomainName}.${props.domainName}`,
            AUTH_TRUST_HOST: 'true',
            STORAGE_BUCKET: gameAssetsBucket.bucketName,
            STORAGE_CDN_URL: `https://${cloudfrontDistribution.distributionDomainName}`,
            DYNAMODB_TABLE_NAME: table.tableName,
            DEFAULT_GAME_SKELETON: props.defaultGameSkeleton || 'SubwaySurfers',
            // Cognito Environment Variables
            COGNITO_USER_POOL_ID: cognito.userPool.userPoolId,
            COGNITO_CLIENT_ID: cognito.userPoolClient.userPoolClientId,
            COGNITO_REGION: cdk.Stack.of(this).region,
            COGNITO_ISSUER_URL: `https://cognito-idp.${cdk.Stack.of(this).region}.amazonaws.com/${cognito.userPool.userPoolId}`,
            COGNITO_DOMAIN: `https://${cognito.userPoolDomain.domainName}.auth.${cdk.Stack.of(this).region}.amazoncognito.com`,
            NEXT_PUBLIC_COGNITO_CLIENT_ID: cognito.userPoolClient.userPoolClientId,
            NEXT_PUBLIC_COGNITO_DOMAIN: `https://${cognito.userPoolDomain.domainName}.auth.${cdk.Stack.of(this).region}.amazoncognito.com`,
            // Analytics Environment Variables (runtime)
            NEXT_PUBLIC_SENTRY_ENVIRONMENT: process.env.NEXT_PUBLIC_SENTRY_ENVIRONMENT || 'dev',
            NEXT_PUBLIC_BUILD_ID: process.env.NEXT_PUBLIC_BUILD_ID || 'dev',
          },
          secrets: {
            OPENAI_API_KEY: ecs.Secret.fromSecretsManager(
              appSecret,
              'OPENAI_API_KEY',
            ),
            AUTH_SECRET: ecs.Secret.fromSecretsManager(
              appSecret,
              'AUTH_SECRET',
            ),
            GOOGLE_CLIENT_ID: ecs.Secret.fromSecretsManager(
              appSecret,
              'GOOGLE_CLIENT_ID',
            ),
            GOOGLE_CLIENT_SECRET: ecs.Secret.fromSecretsManager(
              appSecret,
              'GOOGLE_CLIENT_SECRET',
            ),
            COGNITO_CLIENT_SECRET: ecs.Secret.fromSecretsManager(
              appSecret,
              'COGNITO_CLIENT_SECRET',
            ),
            // Analytics secrets
            NEXT_PUBLIC_SENTRY_DSN: ecs.Secret.fromSecretsManager(
              appSecret,
              'NEXT_PUBLIC_SENTRY_DSN',
            ),
            NEXT_PUBLIC_MIXPANEL_TOKEN: ecs.Secret.fromSecretsManager(
              appSecret,
              'NEXT_PUBLIC_MIXPANEL_TOKEN',
            ),
          },
          logDriver: ecs.LogDrivers.awsLogs({
            streamPrefix: 'nextjs-app',
          }),
        },
        assignPublicIp: true,
        publicLoadBalancer: true,
        certificate: acm.Certificate.fromCertificateArn(
          this,
          'Certificate',
          props.certificateArn,
        ),
      },
    );
    gameAssetsBucket.grantReadWrite(fargateService.taskDefinition.taskRole);
    table.grantReadWriteData(fargateService.taskDefinition.taskRole);
    return fargateService;
  }

  private configureHealthCheck(
    fargateService: ecsp.ApplicationLoadBalancedFargateService,
  ): void {
    fargateService.targetGroup.configureHealthCheck({
      path: '/ping',
      interval: cdk.Duration.seconds(60),
      timeout: cdk.Duration.seconds(15),
    });
  }
}
