// Simple, flexible analytics types (no enterprise complexity)

export interface AnalyticsUser {
    id: string;
    email?: string;
    name?: string;
    type?: 'free' | 'premium' | 'admin';
}

export interface AnalyticsEvent {
    eventName: string;
    timestamp?: number;
    [key: string]: any; // Flexible properties
}

export interface BuildConfig {
    environment: string;
    buildId: string;
    isDevelopment: boolean;
    isStaging: boolean;
    isProduction: boolean;
} 