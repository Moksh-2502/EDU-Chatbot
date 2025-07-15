import type { NextAuthConfig } from 'next-auth';
import CognitoProvider from 'next-auth/providers/cognito';

export const authConfig = {
  pages: {
    signIn: '/login',
    newUser: '/',
  },
  providers: [
    CognitoProvider({
      clientId: process.env.COGNITO_CLIENT_ID,
      clientSecret: process.env.COGNITO_CLIENT_SECRET,
      issuer: `https://cognito-idp.us-east-1.amazonaws.com/${process.env.COGNITO_USER_POOL_ID}`,
      checks: ['nonce'],
    }),
  ],
  callbacks: {},
  trustHost: true,
} satisfies NextAuthConfig;
