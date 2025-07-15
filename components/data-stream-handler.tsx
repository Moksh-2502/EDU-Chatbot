'use client';

import { useEffect } from 'react';
import { useDataStreamContext } from '@/context/data-stream-context';
import { toast } from './toast';

interface DataStreamHandlerProps {
  id: string;
}

type GameCreatedEvent = {
  type: 'game-created';
  content: {
    chatId: string;
    templateName: string;
    gameUrl: string;
  };
};

type GameModifiedEvent = {
  type: 'game-modified';
  content: {
    chatId: string;
    filePath: string;
  };
};

type DataStreamEvent =
  | GameCreatedEvent
  | GameModifiedEvent
  | {
      type: string;
      content: unknown;
    };

export function DataStreamHandler({ id }: DataStreamHandlerProps) {
  const { createListener, removeListener } = useDataStreamContext();

  useEffect(() => {
    const handleDataStream = (data: DataStreamEvent) => {
      if (data.type === 'game-created') {
        const { templateName } = data.content as GameCreatedEvent['content'];

        toast({
          type: 'success',
          description: `Your ${templateName} game is ready to play!`,
        });
      } else if (data.type === 'game-modified') {
        const { filePath } = data.content as GameModifiedEvent['content'];
        toast({
          type: 'success',
          description: `The game file ${filePath} has been updated.`,
        });
      }
    };

    const listenerId = createListener(handleDataStream);

    return () => {
      removeListener(listenerId);
    };
  }, [createListener, removeListener, id]);

  return null;
}
