import type { Metadata } from 'next';
import Link from 'next/link';

export const metadata: Metadata = {
  title: 'Game Playground',
  description: 'View your AI-generated games',
};

export default function GamesIndexPage() {
  return (
    <div className="container flex items-center justify-center flex-col min-h-screen py-8 text-center">
      <h1 className="text-4xl font-bold mb-4">Game Playground</h1>
      <p className="text-muted-foreground mb-8 max-w-md">
        Access your games directly through their unique game URL.
      </p>
      <p>
        <Link href="/" className="text-primary hover:underline">
          Return to home
        </Link>
      </p>
    </div>
  );
}
