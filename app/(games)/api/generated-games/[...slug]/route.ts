import { GameStorageFactory } from '@/lib/services/game-storage/game-storage-factory';
import type { NextRequest } from 'next/server';
import { NextResponse } from 'next/server';

export async function GET(
  _: NextRequest,
  { params }: { params: Promise<{ slug: string[] }> },
) {
  const { slug } = await params;
  if (!slug || slug.length === 0) {
    return NextResponse.json(
      { error: 'File path is missing.' },
      { status: 400 },
    );
  }
  const gameId = slug[0];
  const filePath = slug.slice(1).join('/');

  try {
    const gameStorage = GameStorageFactory.createDefaultStorage();
    const result = await gameStorage.serveGameFile(gameId, filePath);

    if ('redirectUrl' in result) {
      return NextResponse.redirect(result.redirectUrl, {
        status: 302,
        headers: {
          'Cache-Control': 'no-cache, no-store, must-revalidate',
          'Content-Type': result.contentType,
        },
      });
    }

    const headers = new Headers();
    headers.set('Content-Type', result.contentType);
    if (result.contentLength) {
      headers.set('Content-Length', result.contentLength.toString());
    }

    return new NextResponse(result.stream as any, {
      status: 200,
      headers: headers,
    });
  } catch (error: any) {
    if (
      error.message?.includes('File not found') ||
      error.name === 'NoSuchKey'
    ) {
      console.warn(`File not found: ${gameId}/${filePath}`);
      return NextResponse.json({ error: 'File not found.' }, { status: 404 });
    }
    console.error(`Error fetching file: ${gameId}/${filePath}`, error);
    return NextResponse.json(
      { error: 'Failed to retrieve file.' },
      { status: 500 },
    );
  }
}
