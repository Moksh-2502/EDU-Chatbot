import { NextResponse, type NextRequest } from 'next/server';
import { GameStorageFactory } from '@/lib/services/game-storage/game-storage-factory';

export async function GET(
  _: NextRequest,
  { params }: { params: Promise<{ templateName: string; filePath: string[] }> },
) {
  const { templateName, filePath: filePathSegments } = await params;

  if (!templateName || !filePathSegments || filePathSegments.length === 0) {
    return NextResponse.json(
      { error: 'Template name or file path is missing.' },
      { status: 400 },
    );
  }

  const relativeFilePath = filePathSegments.join('/');

  try {
    const storage = GameStorageFactory.createDefaultStorage();
    const skeleton = await storage.getGameSkeletonByTemplate(templateName);

    if (!skeleton) {
      return NextResponse.json(
        { error: `Game skeleton template "${templateName}" not found.` },
        { status: 404 },
      );
    }

    const fileExists = await storage.skeletonFileExists(
      templateName,
      relativeFilePath,
    );

    if (!fileExists) {
      return NextResponse.json(
        {
          error: `File "${relativeFilePath}" not found in template "${templateName}".`,
        },
        { status: 404 },
      );
    }

    const result = await storage.serveSkeletonFile(
      templateName,
      relativeFilePath,
    );

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
    headers.set('Cache-Control', 'public, max-age=3600');

    return new NextResponse(result.stream as any, {
      status: 200,
      headers: headers,
    });
  } catch (error: any) {
    if (error.message?.includes('File not found')) {
      console.warn(`File not found: ${templateName}/${relativeFilePath}`);
      return NextResponse.json({ error: 'File not found.' }, { status: 404 });
    }
    console.error(
      `Error fetching skeleton file: ${templateName}/${relativeFilePath}`,
      error,
    );
    return NextResponse.json(
      { error: 'Failed to retrieve skeleton file.' },
      { status: 500 },
    );
  }
}
