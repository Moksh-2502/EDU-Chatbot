(function (global, factory) {
    typeof exports === 'object' && typeof module !== 'undefined' ? factory(exports) :
    typeof define === 'function' && define.amd ? define(['exports'], factory) :
    (global = typeof globalThis !== 'undefined' ? globalThis : global || self, factory(global.fluencysdk = {}));
})(this, (function (exports) { 'use strict';

    class SimpleGenerator {
        /**
         * Creates a new SimpleGenerator with optional configuration
         * @param config Optional configuration for the generator
         * @param storageKey Key to use for storage (defaults to 'fluencyState')
         * @param storageAdapter Custom storage adapter for platforms without localStorage
         */
        constructor(config, storageKey = 'fluencyState', storageAdapter) {
            this.storageKey = storageKey;
            this.state = {
                currentPosition: 0,
                learnedFacts: {},
                mode: 'learning'
            };
            this.defaultSequence = [0, 1, 10, 2, 5, 3, 4, 6, 7, 8, 9, 11, 12];
            this.questions = {};
            this.config = {
                sequence: config?.sequence || this.defaultSequence,
                maxFactor: config?.maxFactor || 12,
                questionsPerBlock: config?.questionsPerBlock || 5
            };
            // Use provided storage adapter or default to localStorage
            this.storage = storageAdapter || {
                getItem: (key) => {
                    return typeof localStorage !== 'undefined' ? localStorage.getItem(key) : null;
                },
                setItem: (key, value) => {
                    if (typeof localStorage !== 'undefined') {
                        localStorage.setItem(key, value);
                    }
                },
                removeItem: (key) => {
                    if (typeof localStorage !== 'undefined') {
                        localStorage.removeItem(key);
                    }
                }
            };
            this.loadState();
        }
        resetState() {
            this.state = {
                currentPosition: 0,
                learnedFacts: {},
                mode: 'learning'
            };
        }
        generateQuestion(focus) {
            const id = `q_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`;
            let factors;
            if (this.state.mode === 'learning') {
                // In learning mode, one factor is always the current focus
                const otherFactor = Math.floor(Math.random() * (this.config.maxFactor || 12) + 1);
                factors = [focus, otherFactor];
            }
            else {
                // In placement or reinforcement, choose random factors
                const factor1 = Math.floor(Math.random() * (this.config.maxFactor || 12) + 1);
                const factor2 = Math.floor(Math.random() * (this.config.maxFactor || 12) + 1);
                factors = [factor1, factor2];
            }
            const question = {
                id,
                factors,
                answer: factors[0] * factors[1]
            };
            this.questions[id] = question;
            return question;
        }
        /**
         * Gets the next block of questions based on the current state
         * @returns Promise that resolves to an array of Question objects
         */
        async getNextQuestionBlock() {
            const questions = [];
            const questionsPerBlock = this.config.questionsPerBlock || 5;
            if (this.state.mode === 'learning') {
                // In learning mode, focus on current position in sequence
                const currentFocus = this.config.sequence?.[this.state.currentPosition] || 0;
                for (let i = 0; i < questionsPerBlock; i++) {
                    questions.push(this.generateQuestion(currentFocus));
                }
            }
            else if (this.state.mode === 'placement') {
                // In placement mode, sample across the entire range
                for (let i = 0; i < questionsPerBlock; i++) {
                    const randomIndex = Math.floor(Math.random() * (this.config.sequence?.length || 12));
                    const focus = this.config.sequence?.[randomIndex] || randomIndex;
                    questions.push(this.generateQuestion(focus));
                }
            }
            else {
                // In reinforcement mode, use previously learned facts
                // Simple implementation just uses random facts for now
                for (let i = 0; i < questionsPerBlock; i++) {
                    const maxPosition = Math.min(this.state.currentPosition, (this.config.sequence?.length || 12) - 1);
                    const randomIndex = Math.floor(Math.random() * (maxPosition + 1));
                    const focus = this.config.sequence?.[randomIndex] || randomIndex;
                    questions.push(this.generateQuestion(focus));
                }
            }
            // Set timestamp when questions are presented
            questions.forEach(q => {
                q.timeStarted = Date.now();
                this.questions[q.id] = q;
            });
            return questions;
        }
        /**
         * Submits an answer for a given question and updates the state accordingly
         * @param questionId The ID of the question being answered
         * @param answer The user's answer
         * @param responseTimeMs The response time in milliseconds
         * @returns Promise that resolves to an object indicating if the answer was correct and the correct answer
         */
        async submitAnswer(questionId, answer, responseTimeMs) {
            const question = this.questions[questionId];
            if (!question) {
                return { isCorrect: false };
            }
            question.userAnswer = answer;
            question.timeEnded = Date.now();
            question.isCorrect = answer === question.answer;
            // Update learned facts
            const factKey = `${question.factors[0]}x${question.factors[1]}`;
            if (!this.state.learnedFacts[factKey]) {
                this.state.learnedFacts[factKey] = {
                    lastSeen: Date.now(),
                    timesCorrect: 0,
                    timesIncorrect: 0,
                    averageResponseTime: responseTimeMs
                };
            }
            else {
                const fact = this.state.learnedFacts[factKey];
                fact.lastSeen = Date.now();
                if (question.isCorrect) {
                    fact.timesCorrect++;
                }
                else {
                    fact.timesIncorrect++;
                }
                // Update average response time
                const totalResponses = fact.timesCorrect + fact.timesIncorrect;
                fact.averageResponseTime = ((fact.averageResponseTime * (totalResponses - 1)) + responseTimeMs) / totalResponses;
            }
            // In learning mode, advance position if enough correct answers
            if (this.state.mode === 'learning' &&
                question.isCorrect &&
                question.factors.includes(this.config.sequence?.[this.state.currentPosition] || 0)) {
                const currentFocus = this.config.sequence?.[this.state.currentPosition] || 0;
                const factKeys = [];
                for (let i = 0; i <= (this.config.maxFactor || 12); i++) {
                    factKeys.push(`${currentFocus}x${i}`);
                    factKeys.push(`${i}x${currentFocus}`);
                }
                // Count correct answers for current focus
                let correctCount = 0;
                let requiredCount = 5; // Arbitrary threshold
                factKeys.forEach(key => {
                    if (this.state.learnedFacts[key]?.timesCorrect > 0) {
                        correctCount++;
                    }
                });
                if (correctCount >= requiredCount) {
                    this.state.currentPosition++;
                }
            }
            await this.saveState();
            return {
                isCorrect: question.isCorrect,
                correctAnswer: question.answer
            };
        }
        /**
         * Saves the current state to storage
         * @returns Promise that resolves when the state has been saved
         */
        async saveState() {
            this.storage.setItem(this.storageKey, JSON.stringify(this.state));
        }
        /**
         * Loads the state from storage
         * @returns Promise that resolves when the state has been loaded
         */
        async loadState() {
            const savedState = this.storage.getItem(this.storageKey);
            if (savedState) {
                try {
                    this.state = JSON.parse(savedState);
                }
                catch (e) {
                    console.error('Error loading saved state:', e);
                    this.resetState();
                }
            }
            else {
                this.resetState();
            }
        }
        /**
         * Gets the current state
         * @returns Promise that resolves to a copy of the current state
         */
        async getState() {
            return { ...this.state };
        }
        /**
         * Sets the state
         * @param state The new state
         * @returns Promise that resolves when the state has been set
         */
        async setState(state) {
            this.state = { ...state };
            await this.saveState();
        }
        /**
         * Resets the state to the initial values
         * @returns Promise that resolves when the state has been reset
         */
        async reset() {
            this.resetState();
            await this.saveState();
        }
        /**
         * Sets the learning mode
         * @param mode The mode to set
         * @returns Promise that resolves when the mode has been set
         */
        async setMode(mode) {
            this.state.mode = mode;
            await this.saveState();
        }
    }

    /**
     * ApiGenerator implements the FluencyGenerator interface using API calls
     * to a remote server for all operations.
     */
    class ApiGenerator {
        /**
         * Creates a new ApiGenerator
         * @param apiBaseUrl The base URL for the API
         * @param apiKey Optional API key for authentication
         */
        constructor(apiBaseUrl, apiKey) {
            this.apiBaseUrl = apiBaseUrl.endsWith('/') ? apiBaseUrl : `${apiBaseUrl}/`;
            this.apiKey = apiKey;
        }
        /**
         * Makes an authenticated API request
         * @param endpoint The API endpoint to call
         * @param method The HTTP method to use
         * @param data The request body data
         * @returns Promise that resolves to the response data
         */
        async apiRequest(endpoint, method, data) {
            const url = `${this.apiBaseUrl}${endpoint}`;
            const headers = {
                'Content-Type': 'application/json',
            };
            if (this.apiKey) {
                headers['Authorization'] = `Bearer ${this.apiKey}`;
            }
            if (this.sessionId) {
                headers['X-Session-ID'] = this.sessionId;
            }
            const response = await fetch(url, {
                method,
                headers,
                body: data ? JSON.stringify(data) : undefined,
            });
            if (!response.ok) {
                throw new Error(`API request failed: ${response.status} ${response.statusText}`);
            }
            return await response.json();
        }
        /**
         * Gets the next block of questions from the API
         * @returns Promise that resolves to an array of Question objects
         */
        async getNextQuestionBlock() {
            const response = await this.apiRequest('questions', 'GET');
            if (response.sessionId) {
                this.sessionId = response.sessionId;
            }
            return response.questions;
        }
        /**
         * Submits an answer to the API
         * @param questionId The ID of the question being answered
         * @param answer The user's answer
         * @param responseTimeMs The response time in milliseconds
         * @returns Promise that resolves to an object indicating if the answer was correct and the correct answer
         */
        async submitAnswer(questionId, answer, responseTimeMs) {
            return this.apiRequest('answers', 'POST', {
                questionId,
                answer,
                responseTimeMs
            });
        }
        /**
         * Gets the current state from the API
         * @returns Promise that resolves to the current StudentState
         */
        async getState() {
            // Use cached state if available to reduce API calls
            if (this.cachedState) {
                return { ...this.cachedState };
            }
            const state = await this.apiRequest('state', 'GET');
            this.cachedState = state;
            return { ...state };
        }
        /**
         * Sets the state via the API
         * @param state The new state
         * @returns Promise that resolves when the state has been set
         */
        async setState(state) {
            await this.apiRequest('state', 'PUT', state);
            this.cachedState = { ...state };
        }
        /**
         * Resets the state via the API
         * @returns Promise that resolves when the state has been reset
         */
        async reset() {
            await this.apiRequest('reset', 'POST');
            this.cachedState = undefined;
        }
        /**
         * Sets the learning mode via the API
         * @param mode The mode to set
         * @returns Promise that resolves when the mode has been set
         */
        async setMode(mode) {
            await this.apiRequest('mode', 'PUT', { mode });
            // Update cached state if available
            if (this.cachedState) {
                this.cachedState.mode = mode;
            }
        }
    }

    /**
     * Factory for creating FluencyGenerator instances
     */
    class FluencyGeneratorFactory {
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

    /**
     * Calculates the response time in milliseconds
     * @param startTime The start time in milliseconds
     * @returns The response time in milliseconds
     */
    function calculateResponseTime(startTime) {
        return Date.now() - startTime;
    }
    /**
     * Gets a formatted display of a question
     * @param question The question object
     * @returns A formatted string (e.g., "3 × 4 = ?")
     */
    function formatQuestion(question) {
        return `${question.factors[0]} × ${question.factors[1]} = ?`;
    }
    /**
     * Calculates accuracy statistics from a student state
     * @param state The student state
     * @returns An object with accuracy statistics
     */
    function calculateAccuracy(state) {
        let totalCorrect = 0;
        let totalIncorrect = 0;
        Object.values(state.learnedFacts).forEach(fact => {
            totalCorrect += fact.timesCorrect;
            totalIncorrect += fact.timesIncorrect;
        });
        const total = totalCorrect + totalIncorrect;
        const accuracyPercentage = total > 0 ? (totalCorrect / total) * 100 : 0;
        return {
            totalCorrect,
            totalIncorrect,
            accuracyPercentage
        };
    }
    /**
     * Gets the current learning focus based on state
     * @param state The student state
     * @param sequence The sequence array
     * @returns The current focus number
     */
    function getCurrentFocus(state, sequence) {
        return sequence[state.currentPosition] || 0;
    }
    /**
     * Creates a custom storage adapter for platforms that don't have localStorage
     * @param getItemFn Function to get an item
     * @param setItemFn Function to set an item
     * @param removeItemFn Function to remove an item
     * @returns A storage adapter
     */
    function createStorageAdapter(getItemFn, setItemFn, removeItemFn) {
        return {
            getItem: getItemFn,
            setItem: setItemFn,
            removeItem: removeItemFn
        };
    }

    exports.ApiGenerator = ApiGenerator;
    exports.FluencyGeneratorFactory = FluencyGeneratorFactory;
    exports.SimpleGenerator = SimpleGenerator;
    exports.calculateAccuracy = calculateAccuracy;
    exports.calculateResponseTime = calculateResponseTime;
    exports.createStorageAdapter = createStorageAdapter;
    exports.formatQuestion = formatQuestion;
    exports.getCurrentFocus = getCurrentFocus;

    Object.defineProperty(exports, '__esModule', { value: true });

}));
//# sourceMappingURL=index.js.map
