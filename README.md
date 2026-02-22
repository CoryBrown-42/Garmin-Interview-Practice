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

## Exercises

| # | Exercise | Category |
|---|----------|----------|
| 1 | GC Optimization (UI Dashboard) | C# / Performance |
| 2 | Golf Ball Trajectory | Vector Math |
| 3 | Terrain Pulse Effect | HLSL / ShaderLab |
| 4 | Asset Pipeline Audit | Editor Scripting |
| 5 | Tile-Based GPU Optimization | Rendering / Mobile |
| 6 | Procedural Golf Course Mesh | Procedural Geometry |
| 7 | Python Texture Batch Tool | Python / Pipeline |
| 8 | Draw Call Batching Analysis | C# / Profiling |
| 9 | UV Distortion (Water Hazard) | HLSL / ShaderLab |
| 10 | Git LFS & Asset Workflow | Pipeline / Version Control |
| 11 | PBR Material Validator | C# / Rendering |
| 12 | LOD Distance Calculator | C# / Optimization |

---

## Interview Prep Tips — Garmin Senior Unity Technical Artist

Everything below is compiled from the actual Garmin job posting, Glassdoor interview reports, Reddit r/TechnicalArtist and r/gamedev communities, and Garmin's careers page. Use it to focus your prep.

### The Role

You'd be joining the **Home Tee Hero** team — Garmin's consumer golf simulator product. This is a **real-time 3D rendering application**, not a data dashboard. You'll be an individual contributor providing **technical leadership and asset development**, rendering high-fidelity golf courses within the constraints of Garmin's hardware.

**Locations:** Olathe, KS (HQ) or Cary, NC  
**Category:** Engineering (not Art/Design — expect engineering rigor)  
**Level:** Senior, 5+ years experience required

### What You'd Actually Be Doing

- **Designing, authoring, and maintaining shaders** for optimized environmental visuals
- Working within **Unity's Built-in Render Pipeline** (not URP or HDRP — likely for performance/legacy reasons on mobile)
- **Profiling and optimizing rendering** on **tile-based GPUs** (iOS and Android — Apple A-series, Qualcomm Adreno)
- Troubleshooting **procedurally generated geometry** issues
- **Debugging real-time rendering** across multiple hardware configurations
- Managing **environmental art pipelines** with **Git version control**
- Building **Python and C# tools** to improve art/asset workflows
- Working with **distributed art teams** — setting requirements, providing feedback, troubleshooting geometry/shader/material/texture issues
- Creating and modifying assets in **Substance, Photoshop, Houdini, Maya, Max, or Blender**
- Writing **documentation** on tools, methods, and engine constraints

### Required Technical Skills

| Skill Area | What They Want |
|------------|---------------|
| **Game Engine** | Demonstrated experience in Unity (or Unreal) |
| **Rendering** | Strong knowledge of real-time rendering fundamentals and optimization |
| **Shaders** | HLSL/ShaderLab, Amplify Shader Editor, Unity Shader Graph |
| **Profiling** | Unity Profiler, Snapdragon Profiler, Xcode Instruments, or similar |
| **Procedural Content** | Procedural geometry generation, runtime instancing, data-driven pipelines |
| **Geometry** | Deep understanding of UVs, normals, tangents, mesh formats |
| **PBR** | PBR rendering workflows — lighting, texture creation |
| **Version Control** | Git workflows, managing large art-heavy repositories (Git LFS) |
| **Programming** | C# (gameplay + tools) and Python (pipeline automation) |
| **Education** | BS in CS, EE, CE, SE, Math, Physics, or related — GPA ≥ 3.0 (they check) |

### Desired (Bonus) Skills

- GPA ≥ 3.5
- Experience with **golf simulators or golf games**
- Familiarity with **HDRP, URP, or custom Unity render pipeline extensions**
- **Houdini procedural modeling** and Houdini-Unity pipeline integration
- Expertise in **3ds Max, Houdini, Substance Designer/Painter, Photoshop**

### The Interview Process

Based on Glassdoor (75+ reports) and Garmin's official responses:

1. **HR phone screen** — recruiter call, basic fit check
2. **Technical video call** — behavioral + technical questions
3. **On-site / CoderPad interview** — hands-on coding exercise (this is what this app prepares you for)
4. **Reference checks** after the interview rounds

**Timeline:** Typically 2–4 weeks from application to decision  
**Difficulty:** Rated ~5/10 (medium) by candidates  
**Overall experience:** 7/10 (favorable)

**How people get in:**
- 26% employee referral
- 20% job sites
- 17% Indeed
- 13% recruiter outreach

### What the Live Coding Interview Covers

From experienced Technical Artists on Reddit who've done these interviews:

- **NOT typically LeetCode.** They're practical, role-specific exercises.
- **Beginner-level data structure use** — arrays, dictionaries/hashmaps, basic string manipulation
- **3D math** — vectors, matrices, dot/cross products, SDF (Signed Distance Functions)
- **Practical tasks** — parse a file, automate an asset workflow, write a simple game logic script
- **Shader-focused roles** lean toward more **3D math** questions
- Content **depends heavily on the job description** — for this Garmin role, expect Unity Built-in RP, mobile optimization, and pipeline tooling

### Key Advice from Interviewers & Candidates

1. **If you're stuck, write pseudocode first.** Interviewers want to see problem-solving ability and software design thinking, not perfect syntax from memory.
2. **Talk through your reasoning.** Explain why you'd use a `Dictionary` vs a `List`, why you'd avoid `string` concatenation in `Update()`, etc.
3. **Know Garmin's products.** Research **Home Tee Hero** before your interview. Mention it. They notice.
4. **Demonstrate understanding of constraints.** This isn't a PC game — it's running on mobile GPUs with strict thermal/battery budgets.
5. **OOP and encapsulation matter.** They want clean, maintainable code — not clever one-liners.
6. **Negotiate salary upfront.** Multiple sources report it's difficult to get raises after hiring at Garmin.

### Tile-Based GPU Essentials (Know This Cold)

Garmin's target hardware uses **tile-based (deferred) rendering** GPUs (Apple GPU, Qualcomm Adreno). Key differences from desktop GPUs:

- **Overdraw is extremely expensive** — each transparent fragment is processed per-tile, not per-pixel
- **Avoid alpha-blending wherever possible** — use alpha-test (cutout) when you can
- **Minimize render target switching** — each switch forces a tile flush
- **Keep shader complexity low** — mobile GPUs have much smaller ALU budgets
- **Avoid dependent texture reads** — compute UVs in the vertex shader, not the fragment shader
- **Texture memory is tight** — compress aggressively (ASTC on iOS, ETC2 on Android)
- **Profiling tools:** Xcode Metal System Trace (iOS), Snapdragon Profiler (Android), Unity Frame Debugger

### Built-in Render Pipeline Notes

The job specifically targets Unity's **Built-in Render Pipeline** (not URP/HDRP). Know these specifics:

- Shaders use **CG/HLSL with ShaderLab** wrapper syntax
- **Surface shaders** vs **vertex/fragment shaders** — know when to use each
- **Multi-pass rendering** — understand how `Tags`, `Pass`, and `SubShader` work
- **Forward rendering path** is likely used (better for mobile, fewer render targets)
- **Lightmapping** and **light probes** for baked lighting (cheaper than real-time)
- **MaterialPropertyBlock** for per-instance properties without breaking batching
- **Static batching** and **GPU instancing** — know the tradeoffs and limits

### Garmin Culture Notes

- Technology and innovation driven, emphasizes collaboration and teamwork
- The Golf/Home Tee Hero division is a **growing investment** — they have 3 TA positions open simultaneously
- Listed under "Engineering" — expect engineering-level rigor and standards
- GPA requirement (3.0 min, 3.5 desired) signals traditional corporate engineering culture
- Work with **distributed art teams** — communication skills matter as much as code
- "Adventure doesn't have to wait for the weekend" — Garmin encourages using their products

## License

MIT
