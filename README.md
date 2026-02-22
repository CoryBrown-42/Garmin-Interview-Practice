# Garmin-Interview-Practice

A Vite + React + TypeScript interview practice app powered by Google Gemini AI.

## Setup

1. **Install dependencies:**
   ```bash
   npm install
   ```

2. **Configure your API key:**
   - Copy the example env file:
     ```bash
     cp .env.example .env
     ```
   - Open `.env` and replace `your_api_key_here` with your Gemini API key from [Google AI Studio](https://aistudio.google.com/apikey).
   - The `.env` file is listed in `.gitignore` and will **not** be committed to version control.

3. **Run the dev server:**
   ```bash
   npm run dev
   ```

## Environment Variables

| Variable | Description |
|---|---|
| `VITE_GEMINI_API_KEY` | Your Google Gemini API key |

> **Note:** Never commit your `.env` file. Use `.env.example` as a reference template.
