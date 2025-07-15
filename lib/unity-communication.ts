import type { UserData } from '@/lib/types/user-data';
import type {
  ReactGameMessage,
  SessionDataMessage,
} from '@/lib/types/unity-bridge';

/**
 * Validates if a path points to a Unity WebGL build
 */
export function isUnityGamePath(gamePath: string): boolean {
  // Unity WebGL builds typically have these file extensions
  const unityExtensions = [
    '.loader.js',
    '.framework.js.unityweb',
    '.wasm.unityweb',
    '.data.unityweb',
  ];
  return unityExtensions.some((ext) => gamePath.includes(ext));
}

/**
 * Detects if an HTML file is a Unity WebGL game by analyzing its content
 */
export async function detectUnityGameFromHtml(
  htmlUrl: string,
): Promise<{ isUnity: boolean; config?: any }> {
  try {
    const response = await fetch(htmlUrl, {
      method: 'GET',
      headers: {
        Accept:
          'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
        'Accept-Language': 'en-US,en;q=0.5',
        'Cache-Control': 'no-cache',
        Pragma: 'no-cache',
      },
      mode: 'cors',
      credentials: 'same-origin',
      redirect: 'follow',
    });

    if (!response.ok) {
      console.warn(
        `Failed to fetch HTML: ${response.status} ${response.statusText}`,
      );
      return { isUnity: false };
    }

    const htmlContent = await response.text();

    // Check for Unity-specific indicators in the HTML
    const unityIndicators = [
      'unity-container',
      'unity-canvas',
      'createUnityInstance',
      'unityShowBanner',
      '.loader.js',
      '.data.unityweb',
      '.framework.js.unityweb',
      '.wasm.unityweb',
    ];

    const hasUnityIndicators = unityIndicators.some((indicator) =>
      htmlContent.includes(indicator),
    );

    if (!hasUnityIndicators) {
      return { isUnity: false };
    }

    // Extract Unity configuration from the HTML
    const config = extractUnityConfigFromHtml(htmlContent, htmlUrl);

    return {
      isUnity: true,
      config,
    };
  } catch (error) {
    console.warn('Failed to fetch or parse HTML for Unity detection:', error);
    return { isUnity: false };
  }
}

/**
 * Extracts Unity configuration from HTML content
 */
function extractUnityConfigFromHtml(htmlContent: string, htmlUrl: string): any {
  try {
    // Extract buildUrl and other config values using regex
    const buildUrlMatch = htmlContent.match(
      /var\s+buildUrl\s*=\s*["']([^"']+)["']/,
    );
    const loaderUrlMatch = htmlContent.match(
      /var\s+loaderUrl\s*=\s*buildUrl\s*\+\s*["']\/([^"']+\.loader\.js)["']/,
    );

    // Also try alternative pattern for loader URL
    const loaderUrlDirectMatch = htmlContent.match(
      /var\s+loaderUrl\s*=\s*["']([^"']+\.loader\.js)["']/,
    );

    // Try to extract the config object to get more details
    const configMatch = htmlContent.match(/var\s+config\s*=\s*{([^}]+)}/s);

    // Extract streaming assets URL from config
    const streamingAssetsMatch = htmlContent.match(
      /streamingAssetsUrl:\s*["']([^"']+)["']/,
    );

    const basePath = htmlUrl.replace('/index.html', '');
    let buildUrl = 'Build';

    if (buildUrlMatch) {
      buildUrl = buildUrlMatch[1];
    }

    // Extract game name from the loader URL
    let gameName = 'dist'; // Default fallback based on the HTML pattern

    if (loaderUrlMatch) {
      // Pattern: buildUrl + "/dist.loader.js"
      const loaderFileName = loaderUrlMatch[1];
      gameName = loaderFileName.replace('.loader.js', '');
    } else if (loaderUrlDirectMatch) {
      // Pattern: "Build/game.loader.js"
      const loaderPath = loaderUrlDirectMatch[1];
      const nameMatch = loaderPath.match(/([^\/]+)\.loader\.js$/);
      if (nameMatch) {
        gameName = nameMatch[1];
      }
    }

    // Extract product name from config if available
    let productName = gameName;
    if (configMatch) {
      const productNameMatch = configMatch[1].match(
        /productName:\s*["']([^"']+)["']/,
      );
      if (productNameMatch) {
        productName = productNameMatch[1];
      }
    }

    // Use streaming assets URL from config or default
    let streamingAssetsUrl = `${basePath}/StreamingAssets`;
    if (streamingAssetsMatch) {
      const streamingPath = streamingAssetsMatch[1];
      if (streamingPath.startsWith('http')) {
        streamingAssetsUrl = streamingPath;
      } else {
        streamingAssetsUrl = `${basePath}/${streamingPath}`;
      }
    }

    const config = {
      loaderUrl: `${basePath}/${buildUrl}/${gameName}.loader.js`,
      dataUrl: `${basePath}/${buildUrl}/${gameName}.data.unityweb`,
      frameworkUrl: `${basePath}/${buildUrl}/${gameName}.framework.js.unityweb`,
      codeUrl: `${basePath}/${buildUrl}/${gameName}.wasm.unityweb`,
      streamingAssetsUrl: streamingAssetsUrl,
      companyName: 'DefaultCompany',
      productName: productName,
      productVersion: '1.0',
    };

    console.log('[Unity Config] Extracted configuration:', config);

    return config;
  } catch (error) {
    console.warn('Failed to extract Unity config from HTML:', error);
    // Fallback to generateUnityConfig
    return generateUnityConfig(htmlUrl);
  }
}

/**
 * Generates Unity WebGL configuration from a base path
 */
export function generateUnityConfig(basePath: string) {
  const cleanPath = basePath.replace('/index.html', '');

  return {
    loaderUrl: `${cleanPath}/Build/${cleanPath.split('/').pop()}.loader.js`,
    dataUrl: `${cleanPath}/Build/${cleanPath.split('/').pop()}.data.unityweb`,
    frameworkUrl: `${cleanPath}/Build/${cleanPath.split('/').pop()}.framework.js.unityweb`,
    codeUrl: `${cleanPath}/Build/${cleanPath.split('/').pop()}.wasm.unityweb`,
    streamingAssetsUrl: `${cleanPath}/StreamingAssets`,
  };
}

/**
 * Creates a session data message for sending to Unity
 */
export function createSessionDataMessage(
  userData: UserData,
): SessionDataMessage {
  return {
    messageType: 'SessionData',
    user: userData,
    timestamp: Date.now(),
    sessionId: generateSessionId(),
  };
}

/**
 * Creates a standardized React game message
 */
export function createReactGameMessage(
  messageType: string,
  additionalProperties?: Record<string, any>,
): ReactGameMessage {
  return {
    messageType,
    timestamp: Date.now(),
    ...additionalProperties,
  };
}

/**
 * Generates a unique session ID
 */
function generateSessionId(): string {
  return `session_${Date.now()}_${Math.random().toString(36).substring(2, 15)}`;
}

/**
 * Safely stringifies JSON data for Unity consumption
 */
export function safeStringify(data: any): string {
  try {
    return JSON.stringify(data);
  } catch (error) {
    console.warn('Failed to stringify data for Unity:', error);
    return '{}';
  }
}

/**
 * Safely parses JSON data from Unity
 */
export function safeParse<T>(jsonString: string): T | null {
  try {
    return JSON.parse(jsonString) as T;
  } catch (error) {
    console.warn('Failed to parse JSON from Unity:', error);
    return null;
  }
}

/**
 * Logs communication events (can be disabled in production)
 */
export function logCommunication(
  direction: 'to-unity' | 'from-unity',
  data: any,
  enabled = true,
) {
  if (!enabled || process.env.NODE_ENV === 'production') return;

  console.log(`[Unity Bridge] ${direction}:`, data);
}
