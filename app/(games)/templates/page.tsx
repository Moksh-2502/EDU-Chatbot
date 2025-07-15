import { auth } from '@/app/(auth)/auth';
import { GameStorageFactory } from '@/lib/services/game-storage';
import type { Metadata } from 'next';
import { redirect } from 'next/navigation';
import Link from 'next/link';

export const metadata: Metadata = {
  title: 'Game Templates',
  description: 'Browse available game templates',
};

export default async function TemplatesPage() {
  const session = await auth();

  if (!session) {
    redirect('/login');
  }

  const gameStorage = GameStorageFactory.createDefaultStorage();
  const skeletons = await gameStorage.getGameSkeletons();

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold mb-8">Game Templates</h1>

      {skeletons.length === 0 ? (
        <p className="text-muted-foreground">No game templates available.</p>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {skeletons.map((skeleton) => (
            <Link
              key={skeleton.directoryPath}
              href={`/templates/${skeleton.directoryPath}`}
              className="block p-6 border rounded-lg hover:shadow-lg transition-shadow"
            >
              <h2 className="text-xl font-semibold mb-2">
                {skeleton.template || skeleton.directoryPath}
              </h2>
              {skeleton.description && (
                <p className="text-muted-foreground">{skeleton.description}</p>
              )}
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
