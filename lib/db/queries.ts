import 'server-only';

import type { VisibilityType } from '@/components/visibility-selector';
import { DynamoDBClient } from '@aws-sdk/client-dynamodb';
import {
  DeleteCommand,
  DynamoDBDocumentClient,
  GetCommand,
  PutCommand,
  QueryCommand,
  UpdateCommand,
} from '@aws-sdk/lib-dynamodb';
import { generateUUID } from '../utils';
import type {
  Chat,
  DynamoDBChat,
  DynamoDBUser,
  Message,
  User,
  Vote,
} from './schema';

const client = new DynamoDBClient({
  region: process.env.AWS_REGION || 'us-east-1',
});

const docClient = DynamoDBDocumentClient.from(client, {
  marshallOptions: {
    convertEmptyValues: false,
    removeUndefinedValues: true,
    convertClassInstanceToMap: false,
  },
});

const TABLE_NAME = process.env.DYNAMODB_TABLE_NAME || '';

function userPK(email: string): string {
  return `USER#${email}`;
}

function chatSK(chatId: string, createdAt: string): string {
  return `CHAT#${chatId}#${createdAt}`;
}

function chatGSI1PK(chatId: string): string {
  return `CHAT#${chatId}`;
}

export async function getUser(email: string): Promise<Array<User>> {
  try {
    const result = await docClient.send(
      new GetCommand({
        TableName: TABLE_NAME,
        Key: {
          PK: userPK(email),
          SK: 'METADATA',
        },
      }),
    );

    if (!result.Item) {
      return [];
    }

    const dynamoUser = result.Item as DynamoDBUser;
    const user: User = {
      id: dynamoUser.id,
      email: dynamoUser.email,
      name: dynamoUser.name,
      createdAt: dynamoUser.createdAt,
    };

    return [user];
  } catch (error) {
    console.error('Failed to get user from database', error);
    throw error;
  }
}

export async function findOrCreateUser(
  userData: Partial<User> & Pick<User, 'email'>,
): Promise<User> {
  try {
    const existingUsers = await getUser(userData.email);

    if (existingUsers.length > 0) {
      return existingUsers[0];
    }

    const userId = userData.id || generateUUID();
    const createdAt = userData.createdAt || new Date().toISOString();

    const dynamoUser: DynamoDBUser = {
      PK: userPK(userData.email),
      SK: 'METADATA',
      id: userId,
      email: userData.email,
      name: userData.name,
      type: 'user',
      createdAt,
    };

    await docClient.send(
      new PutCommand({
        TableName: TABLE_NAME,
        Item: dynamoUser,
      }),
    );

    return {
      id: userId,
      email: userData.email,
      name: userData.name,
      createdAt,
    };
  } catch (error) {
    console.error('Failed to find or create user:', error);
    throw error;
  }
}

export async function saveChat({
  id,
  userId,
  title,
  visibility,
}: {
  id: string;
  userId: string;
  title: string;
  visibility: VisibilityType;
}) {
  try {
    const createdAt = new Date().toISOString();

    const dynamoChat: DynamoDBChat = {
      PK: userPK(userId),
      SK: chatSK(id, createdAt),
      GSI1PK: chatGSI1PK(id),
      GSI1SK: 'METADATA',
      id,
      userId,
      title,
      visibility,
      type: 'chat',
      createdAt,
      messages: [],
    };

    await docClient.send(
      new PutCommand({
        TableName: TABLE_NAME,
        Item: dynamoChat,
      }),
    );
  } catch (error) {
    console.error('Failed to save chat in database', error);
    throw error;
  }
}

export async function getChatById({
  id,
}: { id: string }): Promise<Chat | undefined> {
  try {
    const result = await docClient.send(
      new QueryCommand({
        TableName: TABLE_NAME,
        IndexName: 'GSI1',
        KeyConditionExpression: 'GSI1PK = :pk AND GSI1SK = :sk',
        ExpressionAttributeValues: {
          ':pk': chatGSI1PK(id),
          ':sk': 'METADATA',
        },
        Limit: 1,
      }),
    );

    if (!result.Items || result.Items.length === 0) {
      return undefined;
    }

    const dynamoChat = result.Items[0] as DynamoDBChat;
    return {
      id: dynamoChat.id,
      userId: dynamoChat.userId,
      title: dynamoChat.title,
      visibility: dynamoChat.visibility,
      createdAt: dynamoChat.createdAt,
      messages: dynamoChat.messages || [],
    };
  } catch (error) {
    console.error('Failed to get chat by id from database', error);
    throw error;
  }
}

export async function getChatsByUserId({
  id,
  limit,
  startingAfter,
  endingBefore,
}: {
  id: string;
  limit: number;
  startingAfter: string | null;
  endingBefore: string | null;
}) {
  try {
    const result = await docClient.send(
      new QueryCommand({
        TableName: TABLE_NAME,
        KeyConditionExpression: 'PK = :pk AND begins_with(SK, :skPrefix)',
        ExpressionAttributeValues: {
          ':pk': userPK(id),
          ':skPrefix': 'CHAT#',
        },
        ScanIndexForward: false,
      }),
    );

    const items = (result.Items || []) as DynamoDBChat[];
    const chats: Chat[] = items.map((item) => ({
      id: item.id,
      userId: item.userId,
      title: item.title,
      visibility: item.visibility,
      createdAt: item.createdAt,
      messages: item.messages || [],
    }));

    let filteredChats = chats;

    if (startingAfter) {
      const startIndex = chats.findIndex((chat) => chat.id === startingAfter);
      if (startIndex !== -1) {
        filteredChats = chats.slice(startIndex + 1);
      }
    } else if (endingBefore) {
      const endIndex = chats.findIndex((chat) => chat.id === endingBefore);
      if (endIndex !== -1) {
        filteredChats = chats.slice(0, endIndex);
      }
    }

    filteredChats = filteredChats.sort((a, b) => {
      return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
    });

    const hasMore = filteredChats.length > limit;
    return {
      chats: hasMore ? filteredChats.slice(0, limit) : filteredChats,
      hasMore,
    };
  } catch (error) {
    console.error('Failed to get chats by user from database', error);
    throw error;
  }
}

export async function deleteChatById({ id }: { id: string }) {
  try {
    const chat = await getChatById({ id });
    if (!chat) {
      throw new Error('Chat not found');
    }

    await docClient.send(
      new DeleteCommand({
        TableName: TABLE_NAME,
        Key: {
          PK: userPK(chat.userId),
          SK: chatSK(id, chat.createdAt),
        },
      }),
    );

    return chat;
  } catch (error) {
    console.error('Failed to delete chat by id from database', error);
    throw error;
  }
}

export async function saveMessages({
  messages,
}: {
  messages: Array<Message & { chatId: string }>;
}) {
  try {
    const chatId = messages[0].chatId;
    const chat = await getChatById({ id: chatId });

    if (!chat) {
      throw new Error('Chat not found');
    }

    const updatedMessages = [
      ...chat.messages,
      ...messages.map(({ chatId, ...msg }) => msg),
    ];

    await docClient.send(
      new UpdateCommand({
        TableName: TABLE_NAME,
        Key: {
          PK: userPK(chat.userId),
          SK: chatSK(chatId, chat.createdAt),
        },
        UpdateExpression: 'SET messages = :messages',
        ExpressionAttributeValues: {
          ':messages': updatedMessages,
        },
      }),
    );
  } catch (error) {
    console.error('Failed to save messages in database', error);
    throw error;
  }
}

export async function getMessagesByChatId({
  id,
}: { id: string }): Promise<Message[]> {
  try {
    const chat = await getChatById({ id });

    if (!chat) {
      return [];
    }

    return chat.messages || [];
  } catch (error) {
    console.error('Failed to get messages by chat id from database', error);
    throw error;
  }
}

export async function voteMessage({
  chatId,
  messageId,
  type,
}: {
  chatId: string;
  messageId: string;
  type: 'up' | 'down';
}) {
  try {
    const chat = await getChatById({ id: chatId });

    if (!chat) {
      throw new Error('Chat not found');
    }

    const updatedMessages = chat.messages.map((msg) =>
      msg.id === messageId ? { ...msg, vote: type } : msg,
    );

    await docClient.send(
      new UpdateCommand({
        TableName: TABLE_NAME,
        Key: {
          PK: userPK(chat.userId),
          SK: chatSK(chatId, chat.createdAt),
        },
        UpdateExpression: 'SET messages = :messages',
        ExpressionAttributeValues: {
          ':messages': updatedMessages,
        },
      }),
    );
  } catch (error) {
    console.error('Failed to upvote message in database', error);
    throw error;
  }
}

export async function getVotesByChatId({
  id,
}: { id: string }): Promise<Vote[]> {
  try {
    const chat = await getChatById({ id });

    if (!chat) {
      return [];
    }

    return chat.messages
      .filter((msg) => msg.vote !== undefined)
      .map((msg) => ({
        messageId: msg.id,
        chatId: id,
        isUpvoted: msg.vote === 'up',
      }));
  } catch (error) {
    console.error('Failed to get votes by chat id from database', error);
    throw error;
  }
}

export async function getMessageById({
  id,
}: { id: string }): Promise<Message[]> {
  console.warn('getMessageById is inefficient in single-table design');
  return [];
}

export async function deleteMessagesByChatIdAfterTimestamp({
  chatId,
  timestamp,
}: {
  chatId: string;
  timestamp: Date;
}) {
  try {
    const chat = await getChatById({ id: chatId });

    if (!chat) {
      throw new Error('Chat not found');
    }

    const updatedMessages = chat.messages.filter(
      (msg) => new Date(msg.createdAt) < timestamp,
    );

    await docClient.send(
      new UpdateCommand({
        TableName: TABLE_NAME,
        Key: {
          PK: userPK(chat.userId),
          SK: chatSK(chatId, chat.createdAt),
        },
        UpdateExpression: 'SET messages = :messages',
        ExpressionAttributeValues: {
          ':messages': updatedMessages,
        },
      }),
    );
  } catch (error) {
    console.error(
      'Failed to delete messages by id after timestamp from database',
      error,
    );
    throw error;
  }
}

export async function updateChatVisibilityById({
  chatId,
  visibility,
}: {
  chatId: string;
  visibility: 'private' | 'public';
}) {
  try {
    const chat = await getChatById({ id: chatId });

    if (!chat) {
      throw new Error('Chat not found');
    }

    await docClient.send(
      new UpdateCommand({
        TableName: TABLE_NAME,
        Key: {
          PK: userPK(chat.userId),
          SK: chatSK(chatId, chat.createdAt),
        },
        UpdateExpression: 'SET visibility = :visibility',
        ExpressionAttributeValues: {
          ':visibility': visibility,
        },
      }),
    );
  } catch (error) {
    console.error('Failed to update chat visibility in database', error);
    throw error;
  }
}

export async function getMessageCountByUserId({
  id,
  differenceInHours,
}: { id: string; differenceInHours: number }) {
  try {
    const cutoffTime = new Date(
      Date.now() - differenceInHours * 60 * 60 * 1000,
    );

    const result = await docClient.send(
      new QueryCommand({
        TableName: TABLE_NAME,
        KeyConditionExpression: 'PK = :pk AND begins_with(SK, :skPrefix)',
        ExpressionAttributeValues: {
          ':pk': userPK(id),
          ':skPrefix': 'CHAT#',
        },
      }),
    );

    const chats = (result.Items || []) as DynamoDBChat[];

    let messageCount = 0;
    for (const chat of chats) {
      if (chat.messages) {
        messageCount += chat.messages.filter(
          (msg) => msg.role === 'user' && new Date(msg.createdAt) >= cutoffTime,
        ).length;
      }
    }

    return messageCount;
  } catch (error) {
    console.error(
      'Failed to get message count by user id for the last 24 hours from database',
      error,
    );
    throw error;
  }
}
