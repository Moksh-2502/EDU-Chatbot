import { auth } from '@/app/(auth)/auth';
import { UserDataStorageFactory } from '@/lib/services/user-data-storage';
import type { NextRequest } from 'next/server';
import { NextResponse } from 'next/server';

// GET /api/user-data/[key] - Load user data
export async function GET(
  _: NextRequest,
  { params }: { params: Promise<{ key: string }> },
) {
  const { key } = await params;

  if (!key) {
    return NextResponse.json({ error: 'Key is required' }, { status: 400 });
  }

  const session = await auth();

  if (!session || !session.user || !session.user.id) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const userId = session.user.id;

  try {
    const userDataStorage = UserDataStorageFactory.createDefaultStorage();
    const data = await userDataStorage.load(userId, key);

    if (data === null) {
      return NextResponse.json({ error: 'Data not found' }, { status: 404 });
    }

    return NextResponse.json({ success: true, data });
  } catch (error) {
    console.error(`Error loading user data for ${userId}/${key}:`, error);
    return NextResponse.json(
      { error: 'Failed to load user data' },
      { status: 500 },
    );
  }
}

// POST /api/user-data/[key] - Save user data
export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ key: string }> },
) {
  const { key } = await params;

  if (!key) {
    return NextResponse.json({ error: 'Key is required' }, { status: 400 });
  }

  const session = await auth();

  if (!session || !session.user || !session.user.id) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const userId = session.user.id;

  try {
    const data = await request.json();
    const userDataStorage = UserDataStorageFactory.createDefaultStorage();
    await userDataStorage.save(userId, key, data);

    return NextResponse.json({ success: true });
  } catch (error) {
    console.error(`Error saving user data for ${userId}/${key}:`, error);
    return NextResponse.json(
      { error: 'Failed to save user data' },
      { status: 500 },
    );
  }
}

// DELETE /api/user-data/[key] - Delete user data
export async function DELETE(
  _: NextRequest,
  { params }: { params: Promise<{ key: string }> },
) {
  const { key } = await params;

  if (!key) {
    return NextResponse.json({ error: 'Key is required' }, { status: 400 });
  }

  const session = await auth();

  if (!session || !session.user || !session.user.id) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const userId = session.user.id;

  try {
    const userDataStorage = UserDataStorageFactory.createDefaultStorage();
    await userDataStorage.delete(userId, key);

    return NextResponse.json({ success: true });
  } catch (error) {
    console.error(`Error deleting user data for ${userId}/${key}:`, error);
    return NextResponse.json(
      { error: 'Failed to delete user data' },
      { status: 500 },
    );
  }
}

// HEAD /api/user-data/[key] - Check if user data exists
export async function HEAD(
  _: NextRequest,
  { params }: { params: Promise<{ key: string }> },
) {
  const { key } = await params;

  if (!key) {
    return new NextResponse(null, { status: 400 });
  }

  const session = await auth();

  if (!session || !session.user || !session.user.id) {
    return new NextResponse(null, { status: 401 });
  }

  const userId = session.user.id;

  try {
    const userDataStorage = UserDataStorageFactory.createDefaultStorage();
    const exists = await userDataStorage.exists(userId, key);

    return new NextResponse(null, {
      status: exists ? 200 : 404,
    });
  } catch (error) {
    console.error(
      `Error checking user data existence for ${userId}/${key}:`,
      error,
    );
    return new NextResponse(null, { status: 500 });
  }
}
