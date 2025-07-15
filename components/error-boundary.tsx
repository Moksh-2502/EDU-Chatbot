'use client';

import React, { Component, useCallback } from 'react';
import * as Sentry from '@sentry/nextjs';
import * as analytics from '@/lib/services/analytics';

interface ErrorBoundaryProps {
    children: React.ReactNode;
    fallback?: React.ComponentType<{ error: Error; resetErrorBoundary: () => void }>;
    context?: Record<string, any>;
}

interface ErrorBoundaryState {
    hasError: boolean;
    error: Error | null;
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
    constructor(props: ErrorBoundaryProps) {
        super(props);
        this.state = { hasError: false, error: null };
    }

    static getDerivedStateFromError(error: Error): ErrorBoundaryState {
        return { hasError: true, error };
    }

    componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
        // Log to Sentry with additional context
        Sentry.withScope((scope) => {
            scope.setTag('component', 'ErrorBoundary');
            scope.setContext('errorInfo', {
                componentStack: errorInfo.componentStack,
            });
            scope.setContext('props', this.props.context || {});
            Sentry.captureException(error);
        });

        console.error('ErrorBoundary caught an error:', error, errorInfo);
    }

    resetErrorBoundary = () => {
        this.setState({ hasError: false, error: null });
    };

    render() {
        if (this.state.hasError && this.state.error) {
            const FallbackComponent = this.props.fallback || DefaultErrorFallback;
            return (
                <FallbackComponent
                    error={this.state.error}
                    resetErrorBoundary={this.resetErrorBoundary}
                />
            );
        }

        return this.props.children;
    }
}

function DefaultErrorFallback({ error, resetErrorBoundary }: {
    error: Error;
    resetErrorBoundary: () => void;
}) {
    return (
        <div className="flex flex-col items-center justify-center p-6 bg-red-50 dark:bg-red-950 rounded-lg border border-red-200 dark:border-red-800">
            <div className="text-red-600 dark:text-red-400 text-lg font-semibold mb-2">
                Something went wrong
            </div>
            <div className="text-red-600 dark:text-red-400 text-sm mb-4 text-center">
                {error.message}
            </div>
            <button
                type="button"
                onClick={resetErrorBoundary}
                className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 transition-colors"
            >
                Try again
            </button>
        </div>
    );
}

// Hook-based error handler for functional components
export function useErrorHandler() {
    return useCallback((error: Error, context?: Record<string, any>) => {
        analytics.trackError(error, context);

        // Re-throw the error so it can be caught by error boundaries
        throw error;
    }, []);
}

// Higher-order component to wrap components with error boundary
export function withErrorBoundary<T extends Record<string, any>>(
    Component: React.ComponentType<T>,
    fallback?: React.ComponentType<{ error: Error; resetErrorBoundary: () => void }>,
    context?: Record<string, any>
) {
    const WrappedComponent = (props: T) => (
        <ErrorBoundary fallback={fallback} context={context}>
            <Component {...props} />
        </ErrorBoundary>
    );

    WrappedComponent.displayName = `withErrorBoundary(${Component.displayName || Component.name})`;

    return WrappedComponent;
} 