import { FluencyGenerator, FluencyGeneratorConfig } from './types';
import { StorageAdapter } from './utils';
type GeneratorType = 'simple' | 'api';
interface ApiGeneratorOptions {
    apiBaseUrl: string;
    apiKey?: string;
}
interface GeneratorOptions {
    config?: FluencyGeneratorConfig;
    storageKey?: string;
    storageAdapter?: StorageAdapter;
    apiOptions?: ApiGeneratorOptions;
}
/**
 * Factory for creating FluencyGenerator instances
 */
export declare class FluencyGeneratorFactory {
    /**
     * Creates a new FluencyGenerator of the specified type
     * @param type The type of generator to create
     * @param options Configuration options for the generator
     * @returns A new FluencyGenerator instance
     */
    static create(type: GeneratorType, options?: GeneratorOptions): FluencyGenerator;
}
export {};
