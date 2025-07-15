import { findOrCreateUser } from '@/lib/db/queries';
import NextAuth from 'next-auth';
import type { DefaultJWT } from 'next-auth/jwt';
import { authConfig } from './auth.config';

export type UserType = 'regular';

declare module 'next-auth' {
  interface Session {
    user: {
      id: string;
      type: UserType;
      email: string;
      name?: string | null;
      image?: string | null;
    };
    cognitoIdToken?: string;
    cognitoRefreshToken?: string;
    cognitoAccessToken?: string;
    error?: string;
  }

  interface User {
    id?: string;
    email?: string | null;
    name?: string | null;
    image?: string | null;
    type: UserType;
  }
}

declare module 'next-auth/jwt' {
  interface JWT extends DefaultJWT {
    id: string;
    type: UserType;
    email: string;
    name?: string | null;
    picture?: string | null;
    cognitoIdToken?: string;
    cognitoRefreshToken?: string;
    cognitoAccessToken?: string;
    error?: string;
  }
}

/**
 * Refreshes the Cognito access token using the refresh token
 */
async function refreshCognitoToken(refreshToken: string) {
  try {
    const clientId = process.env.COGNITO_CLIENT_ID;
    const clientSecret = process.env.COGNITO_CLIENT_SECRET;

    if (!clientId || !clientSecret) {
      throw new Error('Missing required Cognito environment variables');
    }

    const response = await fetch(
      `${process.env.COGNITO_DOMAIN}/oauth2/token`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: new URLSearchParams({
          grant_type: 'refresh_token',
          refresh_token: refreshToken,
          client_id: clientId,
          client_secret: clientSecret,
        }),
      }
    );

    const tokens = await response.json();

    if (!response.ok) {
      throw new Error(tokens.error || 'Failed to refresh token');
    }

    return {
      idToken: tokens.id_token,
      accessToken: tokens.access_token,
      refreshToken: tokens.refresh_token || refreshToken, // Use new refresh token if provided
      expiresAt: Date.now() + tokens.expires_in * 1000,
    };
  } catch (error) {
    console.error('Error refreshing Cognito token:', error);
    throw error;
  }
}

/**
 * Checks if a JWT token is expired (with 5 minute buffer)
 */
function isTokenExpired(token: string): boolean {
  try {
    const tokenParts = token.split('.');
    if (tokenParts.length !== 3) return true;

    const payload = JSON.parse(
      atob(tokenParts[1].replace(/-/g, '+').replace(/_/g, '/'))
    );

    const expirationTime = payload.exp * 1000; // Convert to milliseconds
    const now = Date.now();
    const bufferTime = 5 * 60 * 1000; // 5 minutes buffer

    return now >= (expirationTime - bufferTime);
  } catch (error) {
    console.error('Error checking token expiration:', error);
    return true;
  }
}

export const {
  handlers: { GET, POST },
  auth,
  signIn,
  signOut,
} = NextAuth({
  ...authConfig,
  callbacks: {
    async jwt({ token, user, account }) {
      if (user) {
        if (!user.email) {
          console.error(
            'User authentication failed: Email is missing from provider response.',
            { provider: account?.provider },
          );
          throw new Error('Email is required for authentication.');
        }

        const userEmail = user.email as string;
        const userType = 'regular';
        const localUser = await findOrCreateUser({
          email: user.email,
          name: user.name || undefined,
        });
        if (!localUser || !localUser.id || !localUser.email) {
          console.error(
            'User persistence error: findOrCreateUserByEmail returned invalid data.',
            localUser,
          );
          throw new Error('Failed to process user account.');
        }
        const userId = localUser.id;
        if (!userId) {
          console.error('User ID is missing after provider processing.', {
            user,
            account,
          });
          throw new Error('User ID could not be determined.');
        }

        token.id = userId;
        token.email = userEmail;
        token.type = userType;
        token.cognitoIdToken = account?.id_token;
        token.cognitoRefreshToken = account?.refresh_token;
        token.cognitoAccessToken = account?.access_token;
        token.cognitoExpiresAt = Date.now() + (account?.expires_in || 3600) * 1000;
        if (user.name) token.name = user.name;
        if (user.image) token.picture = user.image;
      }

      // Check if tokens need refresh
      if (token.cognitoIdToken && token.cognitoRefreshToken) {
        const shouldRefresh = isTokenExpired(token.cognitoIdToken as string);

        if (shouldRefresh) {
          console.log('Cognito tokens are expired, attempting to refresh...');
          try {
            const refreshedTokens = await refreshCognitoToken(token.cognitoRefreshToken as string);

            // Update token with refreshed values
            token.cognitoIdToken = refreshedTokens.idToken;
            token.cognitoAccessToken = refreshedTokens.accessToken;
            token.cognitoRefreshToken = refreshedTokens.refreshToken;
            token.cognitoExpiresAt = refreshedTokens.expiresAt;

            console.log('Successfully refreshed Cognito tokens');
          } catch (error) {
            console.error('Failed to refresh Cognito tokens:', error);
            // Mark token as having refresh error
            token.error = 'RefreshTokenError';
            // Clear expired tokens to prevent sending them to Unity
            token.cognitoIdToken = undefined;
            token.cognitoAccessToken = undefined;
          }
        }
      }

      if (!token.id || !token.email || !token.type) {
        console.error(
          'Invalid JWT: Essential information (id, email, or type) is missing.',
          token,
        );
        throw new Error('Your session is invalid. Please sign in again.');
      }
      return token;
    },
    async session({ session, token }) {
      session.user.id = token.id;
      session.user.email = token.email;
      session.user.type = token.type;
      if (token.name) session.user.name = token.name;
      if (token.picture) session.user.image = token.picture;
      if (token.cognitoIdToken) session.cognitoIdToken = token.cognitoIdToken;
      if (token.cognitoRefreshToken) session.cognitoRefreshToken = token.cognitoRefreshToken;
      if (token.cognitoAccessToken) session.cognitoAccessToken = token.cognitoAccessToken;
      if (token.error) session.error = token.error;

      return session;
    },
  },
});
