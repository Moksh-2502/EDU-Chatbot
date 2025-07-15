import { S3 } from '@aws-sdk/client-s3';
import { lookup } from 'mime-types';
import fs from 'node:fs/promises';
import path from 'node:path';
import { Readable } from 'node:stream';

const s3 = new S3();

/**
 * Recursively uploads files from a local directory to S3
 * @param sourceDirectory The local directory path
 * @param s3BucketName The target S3 bucket name
 * @param s3KeyPrefix The prefix for S3 keys (e.g., gameId or chatId)
 * @param excludeFiles Optional array of filenames to exclude from uploading
 */
export async function uploadDirectoryToS3(
  sourceDirectory: string,
  s3BucketName: string,
  s3KeyPrefix: string,
  excludeFiles: string[] = ['skeleton-manifest.json'],
) {
  const entries = await fs.readdir(sourceDirectory);

  for (const entryName of entries) {
    if (excludeFiles.includes(entryName)) {
      continue;
    }

    const s3Key = path.join(s3KeyPrefix, entryName).replace(/\\/g, '/');
    const localPath = path.join(sourceDirectory, entryName);
    const fileStat = await fs.stat(localPath);
    if (fileStat.isDirectory()) {
      await uploadDirectoryToS3(localPath, s3BucketName, s3Key, excludeFiles);
    } else if (fileStat.isFile()) {
      try {
        const fileContent = await fs.readFile(localPath);
        await s3.putObject({
          Bucket: s3BucketName,
          Key: s3Key,
          Body: fileContent,
          ContentType: lookup(entryName) || 'application/octet-stream',
        });
      } catch (error) {
        console.error(`Failed to upload ${localPath} to S3:`, error);
        throw error;
      }
    }
  }
}

export interface S3StreamResult {
  stream: ReadableStream<Uint8Array>;
  contentType: string;
  contentLength?: number;
}

export async function getFileFromS3AsWebStream(
  bucketName: string,
  key: string,
): Promise<S3StreamResult> {
  const s3Response = await s3.getObject({
    Bucket: bucketName,
    Key: key,
  });

  if (!s3Response.Body) {
    throw new Error('S3 object body is empty or undefined.');
  }

  if (!(s3Response.Body instanceof Readable)) {
    throw new Error('S3 object body is not a Node.js Readable stream.');
  }

  const s3NodeStream = s3Response.Body;
  const webReadableStream = Readable.toWeb(
    s3NodeStream,
  ) as ReadableStream<Uint8Array>;
  const contentType = lookup(key) || 'application/octet-stream';

  return {
    stream: webReadableStream,
    contentType,
    contentLength: s3Response.ContentLength,
  };
}

export async function checkFileExistsOnS3(
  bucketName: string,
  key: string,
): Promise<boolean> {
  try {
    await s3.headObject({
      Bucket: bucketName,
      Key: key,
    });
    return true;
  } catch (error: any) {
    if (error.name === 'NoSuchKey' || error.name === 'NotFound') {
      return false;
    }
    console.error(`Error checking file existence in S3: ${key}`, error);
    throw error;
  }
}
