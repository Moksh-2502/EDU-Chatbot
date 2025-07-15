import {
  customProvider,
  extractReasoningMiddleware,
  wrapLanguageModel,
} from 'ai';
import { xai } from '@ai-sdk/xai';
import { openai } from '@ai-sdk/openai';

const AI_PROVIDER = process.env.AI_PROVIDER || 'xai';
const OPENAI_MODEL = process.env.OPENAI_MODEL || 'gpt-4o';
const OPENAI_REASONING_MODEL = process.env.OPENAI_REASONING_MODEL || 'gpt-4o';

export const myProvider = customProvider({
  languageModels: {
    'chat-model':
      AI_PROVIDER === 'openai'
        ? openai(OPENAI_MODEL)
        : xai('grok-2-vision-1212'),
    'chat-model-reasoning': wrapLanguageModel({
      model:
        AI_PROVIDER === 'openai'
          ? openai(OPENAI_REASONING_MODEL)
          : xai('grok-3-mini-beta'),
      middleware: extractReasoningMiddleware({ tagName: 'think' }),
    }),
    'title-model':
      AI_PROVIDER === 'openai' ? openai(OPENAI_MODEL) : xai('grok-2-1212'),
  },
  imageModels: {
    'small-model':
      AI_PROVIDER === 'openai'
        ? openai.image(OPENAI_MODEL)
        : xai.image('grok-2-image'),
  },
});
