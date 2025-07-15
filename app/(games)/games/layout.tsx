import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Game Playground',
  description: 'Play your AI-generated games',
};

export default function GamesLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return <div className="min-h-screen flex flex-col">{children}</div>;
}
