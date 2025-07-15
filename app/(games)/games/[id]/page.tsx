import { auth } from '@/app/(auth)/auth';
import { FullScreenGamePreview } from '@/components/full-screen-game-preview';
import { GameStorageFactory } from '@/lib/services/game-storage';
import type { Metadata } from 'next';
import { notFound } from 'next/navigation';

export const metadata: Metadata = {
  title: 'Game View',
  description: 'Play your AI-generated game in full screen',
};

async function doesGameExist(id: string): Promise<boolean> {
  try {
    // Get the configured game storage service
    const gameStorage = GameStorageFactory.createDefaultStorage();
    
    // Check if the index.html file exists for this game
    return await gameStorage.gameFileExists(id, 'index.html');
  } catch (error) {
    console.error(`Error checking if game ${id} exists:`, error);
    return false;
  }
}

export default async function GamePage(props: {
  params: Promise<{ id: string }>;
}) {
  const params = await props.params;
  const session = await auth();

  if (!session?.user) {
    return notFound();
  }
  const { id } = params;

  const gameExists = await doesGameExist(id);
  if (!gameExists) {
    return notFound();
  }

  return <FullScreenGamePreview gameId={id} />;
}
