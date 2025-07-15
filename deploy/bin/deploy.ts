#!/usr/bin/env node
import * as cdk from 'aws-cdk-lib';
import { DeployStack } from '../lib/deploy-stack';

const app = new cdk.App();

const defaultGameSkeleton =
  process.env.CDK_DEFAULT_GAME_SKELETON || 'SubwaySurfers';

const bucketName = process.env.CDK_S3_BUCKET_NAME;

new DeployStack(app, 'AiEduChatDevStack', {
  env: {
    account: '856284715153',
    region: 'us-east-1',
  },
  vpcId: 'vpc-02f5c59565bdd0106',
  secretId: 'ai-first-game-dev/dev/nextjs-secrets',
  certificateArn:
    'arn:aws:acm:us-east-1:856284715153:certificate/2c7230dd-9ebd-4eb8-be80-c6d9d97770d1',
  domainName: 'wseng.rp.devfactory.com',
  subdomainName: 'edu-game-chat-dev',
  defaultGameSkeleton,
  bucketName,
});

new DeployStack(app, 'AiEduChatProdStack', {
  env: {
    account: '010526244253',
    region: 'us-east-1',
  },
  vpcId: 'vpc-082bb1d3eeaf469f8',
  secretId: 'ai-first-game-dev/prod/nextjs-secrets',
  certificateArn:
    'arn:aws:acm:us-east-1:010526244253:certificate/090d3aed-e052-49f4-8bbe-d8ce45a1fe22',
  domainName: 'learnwith.ai',
  subdomainName: 'trashcat',
  defaultGameSkeleton,
  bucketName,
});
