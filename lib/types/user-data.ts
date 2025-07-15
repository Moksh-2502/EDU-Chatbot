export interface UserData {
  id: string;
  email: string;
  name?: string | null;
  type: string;
  image?: string | null;
  cognitoIdToken?: string;
  tokenExpiryTimestamp?: number; // Unix timestamp in milliseconds
}

export interface UserDataPayload {
  user: UserData;
  timestamp: number;
  sessionId?: string;
}

export interface GameUserPreferences {
  language?: string;
  soundEnabled?: boolean;
  difficulty?: 'easy' | 'medium' | 'hard';
  theme?: 'light' | 'dark';
}

export interface ExtendedUserData extends UserData {
  preferences?: GameUserPreferences;
  achievements?: string[];
  gameStats?: Record<string, any>;
} 