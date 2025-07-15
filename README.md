# AI Education Chatbot

An interactive educational platform that combines AI-powered chat capabilities with engaging educational games for enhanced learning experiences.

## Overview

The AI Education Chatbot is a Next.js application that provides an interactive learning environment through:

1. **Intelligent Chat Interface**: A chat system powered by modern AI models that can answer educational questions, provide tutoring, and guide users through learning materials.

2. **Interactive Educational Games**: Integration with web-based and Unity games designed to reinforce learning concepts through interactive gameplay.

3. **Multimodal Learning**: Support for various content types including text, images, and interactive games to accommodate different learning styles.

## Features

- **AI Chat Interface**: Interactive conversational interface with AI tutoring capabilities
- **Educational Game Integration**: Support for both web-based and Unity games
- **Authentication System**: Secure user accounts and session management
- **File Attachments**: Upload and share files during chat sessions
- **Responsive Design**: Works across desktop and mobile devices
- **Theme Support**: Light and dark mode themes
- **Analytics Integration**: Track usage patterns to improve the learning experience
- **Feedback Mechanism**: Built-in feedback system for continuous improvement

## Tech Stack

- **Frontend**: Next.js, React, TypeScript, Tailwind CSS
- **AI Integration**: OpenAI SDK, AI SDK for React
- **Authentication**: NextAuth.js
- **Game Engine**: Unity integration with React Unity WebGL
- **Styling**: Tailwind CSS with custom components
- **State Management**: React Context API, SWR for data fetching
- **Analytics**: Mixpanel for event tracking
- **Error Tracking**: Sentry
- **Database**: AWS DynamoDB
- **Storage**: AWS S3, Vercel Blob Storage

## Installation and Setup

### Prerequisites

- Node.js 20+ installed
- PNPM package manager
- AWS credentials (for DynamoDB and S3 access)
- Environment variables set up according to `.env.example`

### Environment Variables

Create a `.env` file based on the `.env.example` template with the following variables:

```
# See .env.example for required environment variables
```

> **Important**: Never commit your `.env` file to version control as it contains sensitive credentials.

### Installation Steps

1. Clone the repository:
   ```bash
   git clone https://github.com/Moksh-2502/EDU-Chatbot.git
   cd EDU-Chatbot
   ```

2. Install dependencies:
   ```bash
   pnpm install
   ```

3. Start the development server:
   ```bash
   pnpm dev
   ```

4. Open [http://localhost:3000](http://localhost:3000) in your browser to access the application.

## Project Structure

```
├── app/                     # Next.js app directory with route groups
│   ├── (auth)/              # Authentication routes and components
│   ├── (chat)/              # Chat interface and API routes
│   ├── (games)/             # Game rendering and management
│   └── api/                 # API endpoints
├── components/              # React components
├── context/                 # React Context providers
├── hooks/                   # Custom React hooks
├── lib/                     # Core utilities and services
│   ├── ai/                  # AI model integration
│   ├── db/                  # Database queries and schema
│   └── services/            # Service integrations (analytics, etc.)
├── public/                  # Static assets
├── game_skeletons/          # Game templates and components
└── reusable-game-patterns/  # Reusable game logic and components
```

## Key Components

### Chat System

The chat interface uses AI SDK for React to provide an interactive conversation experience. Messages are stored in a database and can be retrieved across sessions. The system supports multimodal inputs including text and file attachments.

### Game Integration

The platform integrates educational games in two ways:

1. **Web-based Games**: Standard HTML5/JavaScript games loaded in iframes
2. **Unity Games**: More complex 3D games using the Unity engine, integrated via React Unity WebGL

Games are detected automatically and rendered with the appropriate interface. The system includes a feedback mechanism for game evaluation.

### Authentication

User authentication is handled through NextAuth.js with support for multiple providers. Protected routes ensure that only authenticated users can access certain features.

## Usage Guide

### Chat Interface

- Start new conversations from the homepage
- Upload files for AI analysis using the attachment button
- View conversation history in the sidebar
- Set visibility options for chats (private/public)

### Games

- Access games through the games section
- Games automatically adapt to screen size and device capabilities
- Use the feedback button to provide input on game experience

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.
