using System;

namespace FluencySDK
{
    public static class FluencyGeneratorFactory
    {
        public enum GeneratorType
        {
            Simple // Add other types like Api, Intermediate, Advanced later
        }

        public static IFluencyGenerator Create(
            GeneratorType type,
            IStorageAdapter storageAdapter,
            string storageKey = "defaultFluencyState",
            FluencyGeneratorConfig config = null)
        {
            if (storageAdapter == null)
            {
                throw new ArgumentNullException(nameof(storageAdapter), "A storage adapter must be provided.");
            }

            config = config ?? new FluencyGeneratorConfig(); // Use default config if null
            storageKey = string.IsNullOrWhiteSpace(storageKey) ? "defaultFluencyState" : storageKey;

            switch (type)
            {
                case GeneratorType.Simple:
                    return new SimpleFluencyGenerator(storageAdapter, storageKey, config);
                // Future types can be added here:
                // case GeneratorType.Api:
                //    return new ApiFluencyGenerator(apiOptions, storageAdapter, storageKey, config);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), $"Unsupported generator type: {type}");
            }
        }

        // Overload for convenience if user wants to use default FileStorageAdapter
        public static IFluencyGenerator Create(
            GeneratorType type,
            string applicationNameForStorage,
            string storageKey = "defaultFluencyState",
            FluencyGeneratorConfig config = null)
        {
            if (string.IsNullOrWhiteSpace(applicationNameForStorage))
            {
                throw new ArgumentNullException(nameof(applicationNameForStorage), "Application name for storage must be provided for default FileStorageAdapter.");
            }

            IStorageAdapter defaultStorageAdapter = new FileStorageAdapter(applicationNameForStorage, storageKey + ".json"); 
            return Create(type, defaultStorageAdapter, storageKey, config);
        }
    }
} 
 