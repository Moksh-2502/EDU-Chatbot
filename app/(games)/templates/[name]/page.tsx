import { auth } from '@/app/(auth)/auth';
import { FullScreenGamePreview } from '@/components/full-screen-game-preview';
import { GameStorageFactory } from '@/lib/services/game-storage';
import type { Metadata } from 'next';
import { notFound, redirect } from 'next/navigation';

export const metadata: Metadata = {
  title: 'Game Template',
  description: 'Preview game template',
};

async function doesSkeletonExist(templateName: string): Promise<boolean> {
  try {
    const gameStorage = GameStorageFactory.createDefaultStorage();
    const skeleton = await gameStorage.getGameSkeletonByTemplate(templateName);
    return skeleton !== null;
  } catch (error) {
    console.error(`Error checking if skeleton ${templateName} exists:`, error);
    return false;
  }
}

export default async function TemplatePage(props: {
  params: Promise<{ name: string }>;
}) {
  const params = await props.params;
  const session = await auth();

  if (!session) {
    redirect('/login');
  }

  const { name } = params;

  const skeletonExists = await doesSkeletonExist(name);
  if (!skeletonExists) {
    return notFound();
  }

  return <FullScreenGamePreview skeleton={name} />;
}
