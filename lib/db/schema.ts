// External-facing models (no DynamoDB implementation details)
export interface User {
  id: string;
  email: string;
  name?: string;
  createdAt: string; // ISO string
}

export interface Chat {
  id: string;
  userId: string;
  title: string;
  visibility: 'public' | 'private';
  createdAt: string; // ISO string
  messages: Message[];
}

export interface Message {
  id: string;
  role: string;
  parts: any[];
  attachments: any[];
  createdAt: string; // ISO string
  vote?: 'up' | 'down'; // Vote is now a property on the message
}

export interface Vote {
  messageId: string;
  chatId: string;
  isUpvoted: boolean;
}

export type VisibilityType = 'public' | 'private';

// Internal DynamoDB models (only used within lib/db)
export interface DynamoDBUser extends User {
  PK: string; // USER#${email}
  SK: string; // METADATA
  type: 'user';
}

export interface DynamoDBChat extends Chat {
  PK: string; // USER#${userId}
  SK: string; // CHAT#${chatId}#${createdAt}
  GSI1PK: string; // CHAT#${chatId}
  GSI1SK: string; // METADATA
  type: 'chat';
}

// Type guards
export function isUser(item: any): item is User {
  return item?.type === 'user';
}

export function isChat(item: any): item is Chat {
  return item?.type === 'chat';
}
