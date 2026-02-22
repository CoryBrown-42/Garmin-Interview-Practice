# Senior Tech Artist Interview Simulator

An AI-powered practice tool for Senior Technical Artist interview preparation. Solve Unity/C# coding exercises and get instant feedback from an AI coach powered by Google's Gemini API.

![React](https://img.shields.io/badge/React-18-blue) ![Vite](https://img.shields.io/badge/Vite-6-purple) ![TailwindCSS](https://img.shields.io/badge/Tailwind-3-06B6D4) ![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6)

## Features

- **Coding Exercises** — Practice GC optimization, vector math, shader logic, and editor scripting
- **AI Code Review** — Submit solutions and get detailed performance feedback
- **AI Hints & Solutions** — Get progressive hints or full model solutions with explanations
- **Concept Explainer** — Deep-dive into the technical concept behind each exercise
- **Performance Audit** — Run an AI audit on your code for mobile/embedded bottlenecks
- **Custom Exercise Generator** — Describe a topic and the AI creates a new exercise for you

## Prerequisites

- [Node.js](https://nodejs.org/) (v18 or higher)
- A [Google Gemini API key](https://aistudio.google.com/apikey) (free tier available)

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/your-username/tech-artist-interview-simulator.git
cd tech-artist-interview-simulator
```

### 2. Install dependencies

```bash
npm install
```

### 3. Set up your API key

Copy the example environment file and add your Gemini API key:

```bash
cp .env.example .env
```

Then edit `.env` and replace `your_api_key_here` with your actual key:

```
VITE_GEMINI_API_KEY=your_actual_api_key
```

> **Note:** The `.env` file is git-ignored and will not be committed.

### 4. Start the dev server

```bash
npm run dev
```

The app will be available at **http://localhost:5173**.

## Building for Production

```bash
npm run build
```

The output will be in the `dist/` folder, ready to deploy to any static hosting (Vercel, Netlify, GitHub Pages, etc.).

## Project Structure

```
├── index.html              # HTML entry point
├── package.json
├── vite.config.ts          # Vite configuration
├── tailwind.config.js      # Tailwind CSS configuration
├── tsconfig.json           # TypeScript configuration
├── .env.example            # Environment variable template
└── src/
    ├── main.tsx            # React entry point
    ├── App.tsx             # Main application component
    ├── index.css           # Tailwind directives
    └── vite-env.d.ts       # Vite type declarations
```

## Tech Stack

- **React 18** — UI framework
- **TypeScript** — Type safety
- **Vite** — Build tool and dev server
- **Tailwind CSS** — Utility-first styling
- **Lucide React** — Icons
- **Google Gemini API** — AI-powered feedback and exercise generation

## License

MIT
