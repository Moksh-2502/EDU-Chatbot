import { registerOTel } from '@vercel/otel';
import * as Sentry from '@sentry/nextjs';

export async function register() {
  registerOTel({ serviceName: 'ai-chatbot' });

  // Load server-side Sentry configuration
  if (process.env.NEXT_RUNTIME === 'nodejs') {
    await import('./sentry.server.config');
  }

  // Load edge runtime configuration if needed
  if (process.env.NEXT_RUNTIME === 'edge') {
    await import('./sentry.edge.config');
  }
}

export const onRequestError = Sentry.captureRequestError;
