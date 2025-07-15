import { type DataStreamWriter, tool } from 'ai';
import type { Session } from 'next-auth';
import { z } from 'zod';
import { GameStorageFactory } from '@/lib/services/game-storage/game-storage-factory';
import { generateUUID } from '@/lib/utils';

interface ToolProps {
  session: Session;
  dataStream: DataStreamWriter;
}

/**
 * Creates a game from a template
 */
export const createGame = ({ dataStream }: ToolProps) =>
  tool({
    description: 'Create a game for educational purposes based on a template',
    parameters: z.object({
      templateName: z.string().describe('The name of the game template to use'),
    }),
    execute: async ({ templateName }) => {
      const gameId = generateUUID();
      try {
        const gameStorage = GameStorageFactory.createDefaultStorage();
        const templateDetails =
          await gameStorage.getGameSkeletonByTemplate(templateName);

        if (!templateDetails) {
          return {
            success: false,
            error: `Game template "${templateName}" not found`,
            internal_note: `Do not show this JSON response to the user. Instead, tell them you couldn\'t create that specific game type, but suggest another available game type that might be suitable for their needs. Do not mention template names explicitly.`,
          };
        }

        const templateDescription =
          templateDetails.description || 'educational game';

        try {
          await gameStorage.createGameFromSkeleton(templateName, gameId);
        } catch (uploadError) {
          console.error(
            `Failed to create game ${gameId} from skeleton ${templateName}:`,
            uploadError,
          );
          return {
            success: false,
            error: 'Failed to create game assets. Please try again later.',
          };
        }

        const baseUrl = process.env.NEXT_PUBLIC_APP_URL || '';
        const gameUrl = `${baseUrl}/games/${gameId}`;

        dataStream.writeData({
          type: 'game-created',
          content: {
            gameId,
            templateName,
            gameUrl,
          },
        });

        return {
          success: true,
          message: 'Game created successfully',
          gameUrl,
          gameDescription: templateDescription,
          internalNote: `Do not show this JSON response to the user. Instead, tell them their game is ready to play. Do not mention template names or technical details. Point the user to open the provided game URL.`,
        };
      } catch (error) {
        console.error('Error creating game:', error);
        return {
          success: false,
          error: 'Failed to create game due to an internal error',
          internalNote: `Do not show this JSON response to the user. Instead, apologize and tell them there was a problem creating the game. Ask if they would like to try a different type of game instead.`,
        };
      }
    },
  });
