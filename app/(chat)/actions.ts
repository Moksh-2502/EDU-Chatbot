'use server';

import { myProvider } from '@/lib/ai/providers';
import {
  deleteMessagesByChatIdAfterTimestamp,
  updateChatVisibilityById,
} from '@/lib/db/queries';
import { generateText, type UIMessage } from 'ai';
import { cookies } from 'next/headers';

export async function saveChatModelAsCookie(model: string) {
  const cookieStore = await cookies();
  cookieStore.set('chat-model', model);
}

export async function generateTitleFromUserMessage({
  message,
}: {
  message: UIMessage;
}) {
  const { text: title } = await generateText({
    model: myProvider.languageModel('title-model'),
    system: `\n
    - you will generate a short title based on the first message a user begins a conversation with
    - ensure it is not more than 80 characters long
    - the title should be a summary of the user's message
    - do not use quotes or colons`,
    prompt: JSON.stringify(message),
  });

  return title;
}

export async function deleteTrailingMessages({
  chatId,
  messageCreatedAt,
}: {
  chatId: string;
  messageCreatedAt: Date;
}) {
  await deleteMessagesByChatIdAfterTimestamp({
    chatId,
    timestamp: messageCreatedAt,
  });
}

export async function updateChatVisibility({
  chatId,
  visibility,
}: {
  chatId: string;
  visibility: 'private' | 'public';
}) {
  await updateChatVisibilityById({ chatId, visibility });
}
