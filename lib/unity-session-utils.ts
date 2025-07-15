import type { Session } from 'next-auth';
import type { UserData } from './types/user-data';

/**
 * Decodes a JWT token and extracts the expiration time
 */
function getTokenExpirationTimestamp(token: string): number {
  try {
    const tokenParts = token.split('.');
    if (tokenParts.length !== 3) {
      console.warn('Invalid token format');
      return 0;
    }

    const payload = JSON.parse(
      atob(tokenParts[1].replace(/-/g, '+').replace(/_/g, '/')),
    );

    // Return expiration time in milliseconds
    return payload.exp ? payload.exp * 1000 : 0;
  } catch (error) {
    console.error('Error decoding token:', error);
    return 0;
  }
}

/**
 * Checks if a token is expired (with 5 minute buffer)
 */
function isTokenExpired(token: string): boolean {
  const expirationTime = getTokenExpirationTimestamp(token);
  if (expirationTime === 0) return true;

  const now = Date.now();
  const bufferTime = 5 * 60 * 1000; // 5 minutes buffer

  return now >= (expirationTime - bufferTime);
}

/**
 * Gets the time until token expiry in minutes
 */
function getTokenTimeToExpiry(token: string): number {
  const expirationTime = getTokenExpirationTimestamp(token);
  if (expirationTime === 0) return 0;

  const now = Date.now();
  const timeToExpiry = expirationTime - now;

  return Math.max(0, Math.floor(timeToExpiry / (60 * 1000))); // Convert to minutes
}

/**
 * Generates debug information about the authentication state
 */
export function generateAuthDebugInfo(session: Session | null): string {
  if (!session) {
    return 'No active session';
  }

  const userData = createUserDataFromSession(session);
  const debugInfo = [
    `Session ID: ${session.user?.id}`,
    `Email: ${session.user?.email}`,
    `User Type: ${userData.type}`,
    `Has Cognito Token: ${userData.hasCognitoIdToken}`,
    `Token Expiry: ${userData.tokenExpiryTimestamp}`,
    `Token Valid: ${userData.tokenValid}`,
  ];

  return debugInfo.join('\n');
}

/**
 * Creates UserData from NextAuth session
 */
export function createUserDataFromSession(session: Session | null): UserData & {
  hasCognitoIdToken: boolean;
  tokenExpiryTimestamp: number;
  tokenValid: string | boolean;
} {
  let tokenExpiryTimestamp = 0;

  if (session?.cognitoIdToken) {
    tokenExpiryTimestamp = getTokenExpirationTimestamp(session.cognitoIdToken);

    if (isTokenExpired(session.cognitoIdToken)) {
      console.warn('Cognito ID token is expired');
    }

    const timeToExpiry = getTokenTimeToExpiry(session.cognitoIdToken);
    console.log(`Token expires in ${timeToExpiry} minutes`);
  }

  const userData: UserData = {
    id: session?.user?.id || 'anonymous',
    email: session?.user?.email || '',
    name: session?.user?.name || undefined,
    type: session?.user?.type || 'guest',
    image: session?.user?.image || undefined,
    cognitoIdToken: session?.cognitoIdToken || undefined,
    tokenExpiryTimestamp,
  };

  return {
    ...userData,
    hasCognitoIdToken: !!userData.cognitoIdToken,
    tokenExpiryTimestamp,
    tokenValid: userData.cognitoIdToken ? !isTokenExpired(userData.cognitoIdToken) : 'N/A'
  };
}

/**
 * Validates user authentication state
 */
export function validateUserAuthentication(userData: UserData): {
  isValid: boolean;
  reason?: string;
} {
  if (!userData.id || userData.id === 'anonymous') {
    return { isValid: false, reason: 'No user ID' };
  }

  if (!userData.email) {
    return { isValid: false, reason: 'No email address' };
  }

  if (userData.cognitoIdToken) {
    if (isTokenExpired(userData.cognitoIdToken)) {
      return { isValid: false, reason: 'Cognito token expired' };
    }
  }

  return { isValid: true };
}