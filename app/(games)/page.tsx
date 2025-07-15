import { redirect } from 'next/navigation';
import { auth } from '../(auth)/auth';
import { FullScreenGamePreview } from '@/components/full-screen-game-preview';

export default async function Page() {
  const session = await auth();

  if (!session) {
    redirect('/login');
  }

  const defaultSkeleton = process.env.DEFAULT_GAME_SKELETON || 'SubwaySurfers';

  return <FullScreenGamePreview skeleton={defaultSkeleton} />;
}
