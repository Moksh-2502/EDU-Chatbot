# Next.js App AWS Fargate Deployment

This directory contains the AWS CDK stack to deploy the Next.js application to AWS Fargate in development mode.

## Architecture

- **VPC**: Isolated network for the application
- **Fargate Service**: Runs a single container for the Next.js app
- **Application Load Balancer**: Routes traffic to the Fargate service

## Deployment Process

1. Install dependencies:

   ```
   pnpm install
   ```

2. Build the CDK stack:

   ```
   pnpm run build
   ```

3. Deploy the stack:

   ```
   pnpm run cdk deploy
   ```

4. To destroy the stack:
   ```
   pnpm run cdk destroy
   ```

## Important Notes

- The application runs in development mode directly on Fargate's ephemeral storage
- The container uses Node 20 and runs the application with `pnpm dev`
- Any changes made to the filesystem will be lost when the task is restarted
- For production use, consider switching to a build-based deployment

## Customization

To customize the deployment, edit the following files:

- `lib/deploy-stack.ts`: Main infrastructure stack
- `bin/deploy.ts`: Entry point for CDK app
- `../Dockerfile`: Docker image configuration
