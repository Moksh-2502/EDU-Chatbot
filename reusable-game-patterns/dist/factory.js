import { SimpleGenerator } from './generators/SimpleGenerator';
import { ApiGenerator } from './generators/ApiGenerator';
/**
 * Factory for creating FluencyGenerator instances
 */
export class FluencyGeneratorFactory {
    /**
     * Creates a new FluencyGenerator of the specified type
     * @param type The type of generator to create
     * @param options Configuration options for the generator
     * @returns A new FluencyGenerator instance
     */
    static create(type, options = {}) {
        switch (type) {
            case 'simple':
                return new SimpleGenerator(options.config, options.storageKey, options.storageAdapter);
            case 'api':
                if (!options.apiOptions?.apiBaseUrl) {
                    throw new Error('API base URL is required for ApiGenerator');
                }
                return new ApiGenerator(options.apiOptions.apiBaseUrl, options.apiOptions.apiKey);
            default:
                return new SimpleGenerator(options.config, options.storageKey, options.storageAdapter);
        }
    }
}
//# sourceMappingURL=factory.js.map