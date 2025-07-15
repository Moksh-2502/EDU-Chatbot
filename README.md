# Game Generator

## Running locally

You will need to use the environment variables [defined in `.env.example`](.env.example) to run the app locally.

> Note: You should not commit your `.env` file or it will expose secrets that will allow others to control access to your various AI and authentication provider accounts.

You need to have Node 20+ installed and the pnpm package manager. You also need to have the `wsdev` AWS profile set up such that the game can use your credentials for accessing this AWS account: 856284715153, RAM-AWS-Dev-WSEngineering-WSEng.

Then you can run the following commands to start the app:

```bash
pnpm install
pnpm dev
```

The app should now be running on [localhost:3000](http://localhost:3000).
