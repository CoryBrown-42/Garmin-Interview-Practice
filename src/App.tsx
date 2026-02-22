import React, { useState, useEffect, useRef } from 'react';
import { 
  Play, CheckCircle2, ChevronRight, ChevronLeft, 
  RefreshCcw, Lightbulb, Code2, Sparkles, 
  Wand2, MessageSquareText, Zap, BrainCircuit
} from 'lucide-react';
import { initializeApp } from 'firebase/app';
import { getAuth, signInAnonymously } from 'firebase/auth';

// Initial Mock Exercise Data
const INITIAL_EXERCISES = [
  {
    id: 1,
    title: "GC Optimization (UI Dashboard)",
    category: "C# / Performance",
    difficulty: "Medium",
    prompt: "Refactor the 'UpdateDashboard' method to eliminate per-frame garbage collection. Use a StringBuilder or cached references.",
    initialCode: `using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class DashboardController : MonoBehaviour {
    public Text dashboardText;
    
    // TODO: Add cached variables here
    
    void UpdateDashboard(float speed, float fuel, string status) {
        // REFACTOR THIS:
        string displayString = "Speed: " + speed.ToString("F2") + 
                              " | Fuel: " + (fuel * 100).ToString() + "%" +
                              " | System: " + status.ToUpper();
        
        dashboardText.text = displayString;
    }
}`,
    solutionKeywords: ["StringBuilder", "Append", "Clear", "cached", "ToString"]
  },
  {
    id: 2,
    title: "3D Math (Golf Hit Detection)",
    category: "Vector Math",
    difficulty: "Hard",
    prompt: "Determine if a ball velocity is within a 30-degree cone of the screen's normal vector. Return true if valid.",
    initialCode: `using UnityEngine;

public class PhysicsLogic {
    public bool IsValidHit(Vector3 ballVelocity, Vector3 screenNormal) {
        if (ballVelocity.sqrMagnitude < 0.001f) return false;
        
        // Your code here
        return false;
    }
}`,
    solutionKeywords: ["Dot", "Normalize", "Mathf.Cos", "30", "Deg2Rad"]
  },
  {
    id: 3,
    title: "Shader Logic (Pulse Effect)",
    category: "HLSL",
    difficulty: "Medium",
    prompt: "Calculate a 'pulse' factor (0-1) using _Time.y and _PulseSpeed that oscillates smoothly.",
    initialCode: `// HLSL Snippet
fixed4 frag (v2f i) : SV_Target {
    float pulseSpeed = _PulseSpeed;
    
    // Calculate pulse here
    float pulse = 0.0; 
    
    float4 finalColor = lerp(_BaseColor, _HighlightColor, pulse);
    return finalColor;
}`,
    solutionKeywords: ["sin", "cos", "_Time.y", "* 0.5 + 0.5"]
  },
  {
    id: 4,
    title: "Asset Pipeline Audit",
    category: "Editor Scripting",
    difficulty: "Hard",
    prompt: "Write a script that finds all textures in a path and logs those with max size > 512 or mipmaps enabled.",
    initialCode: `using UnityEditor;
using UnityEngine;

public class AssetAuditor {
    public void AuditFolder(string path) {
        string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { path });
        
        // Loop and check maxTextureSize and mipmapEnabled
    }
}`,
    solutionKeywords: ["AssetImporter.GetAtPath", "TextureImporter", "maxTextureSize", "mipmapEnabled"]
  }
];

const apiKey = import.meta.env.VITE_GEMINI_API_KEY || "";

export default function App() {
  const [exercises, setExercises] = useState(INITIAL_EXERCISES);
  const [currentIdx, setCurrentIdx] = useState(0);
  const [code, setCode] = useState(exercises[0].initialCode);
  const [output, setOutput] = useState("");
  const [isAiLoading, setIsAiLoading] = useState(false);
  const [solvedStatus, setSolvedStatus] = useState({});
  const [showAiModal, setShowAiModal] = useState(false);
  const [aiCustomInput, setAiCustomInput] = useState("");

  const exercise = exercises[currentIdx];

  useEffect(() => {
    setCode(exercise.initialCode);
    setOutput("");
  }, [currentIdx, exercises]);

  const handleNext = () => {
    if (currentIdx < exercises.length - 1) setCurrentIdx(currentIdx + 1);
  };

  const handlePrev = () => {
    if (currentIdx > 0) setCurrentIdx(currentIdx - 1);
  };

  const callGemini = async (systemInstruction, userQuery, isJson = true) => {
    setIsAiLoading(true);
    try {
      const response = await fetch(`https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=${apiKey}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          contents: [{ parts: [{ text: userQuery }] }],
          systemInstruction: { parts: [{ text: systemInstruction }] },
          ...(isJson ? { generationConfig: { responseMimeType: "application/json" } } : {})
        })
      });

      if (!response.ok) throw new Error("API Limit reached");
      const data = await response.json();
      const text = data.candidates[0].content.parts[0].text;
      return isJson ? JSON.parse(text) : text;
    } catch (error) {
      console.error(error);
      return null;
    } finally {
      setIsAiLoading(false);
    }
  };

  const checkSolution = async () => {
    setOutput("✨ AI Coach is reviewing your code for performance and correctness...");
    const system = `You are a Senior Unity Tech Artist Interviewer. 
    Evaluate the user's code for: "${exercise.prompt}". 
    Be strict about Garbage Collection and performance.
    Return JSON: { "correct": boolean, "feedback": "string", "hints": ["string"] }`;
    
    const result = await callGemini(system, `Exercise: ${exercise.title}\nCode:\n${code}`);
    if (result) {
      setOutput(result.feedback);
      if (result.correct) setSolvedStatus({ ...solvedStatus, [exercise.id]: true });
    } else {
      setOutput("⚠️ Error connecting to AI Coach. Please try again.");
    }
  };

  const getAiHint = async () => {
    setOutput("✨ Generating a helpful hint...");
    const system = `Provide a subtle hint for the following coding task without giving away the full answer. Be technical. Focus on Unity best practices.`;
    const result = await callGemini(system, `Task: ${exercise.prompt}\nMy Current Code: ${code}`, false);
    setOutput(result || "Could not generate hint.");
  };

  const getSolution = async () => {
    setOutput("✨ Generating model solution and explanation...");
    const system = `Provide the optimal solution for the following Unity Tech Artist task. Explain why this solution is better for a performance-critical environment like Garmin (wearables/embedded).`;
    const result = await callGemini(system, `Task: ${exercise.prompt}`, false);
    setOutput(result || "Could not generate solution.");
  };

  const generateCustomExercise = async () => {
    if (!aiCustomInput) return;
    setIsAiLoading(true);
    const system = `Generate a new technical artist interview coding exercise based on the user's topic. 
    It must be related to Garmin products (Golf simulators, GPS, Aviation, Wearables).
    Include a title, category, difficulty, prompt, and some initial boilerplate code.
    Return JSON: { "id": number, "title": "string", "category": "string", "difficulty": "string", "prompt": "string", "initialCode": "string" }`;
    
    const result = await callGemini(system, `Topic: ${aiCustomInput}`);
    if (result) {
      const newEx = { ...result, id: Date.now() };
      setExercises([...exercises, newEx]);
      setCurrentIdx(exercises.length);
      setShowAiModal(false);
      setAiCustomInput("");
    }
    setIsAiLoading(false);
  };

  return (
    <div className="min-h-screen bg-slate-900 text-slate-100 font-sans p-4 md:p-8">
      {/* AI Custom Task Modal */}
      {showAiModal && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-800 border border-slate-700 rounded-2xl p-6 w-full max-w-md shadow-2xl">
            <div className="flex items-center gap-2 mb-4 text-emerald-400">
              <Sparkles className="w-5 h-5" />
              <h3 className="text-xl font-bold">✨ Generate Custom Task</h3>
            </div>
            <p className="text-slate-400 text-sm mb-4">What Garmin-specific topic do you want to practice? (e.g. "GPS Distance Math", "Aviation PFD UI Optimization", "Golf Swing Physics")</p>
            <textarea 
              className="w-full bg-slate-900 border border-slate-700 rounded-lg p-3 text-sm text-slate-200 focus:ring-2 focus:ring-emerald-500 outline-none mb-4 h-24"
              placeholder="Enter topic..."
              value={aiCustomInput}
              onChange={(e) => setAiCustomInput(e.target.value)}
            />
            <div className="flex gap-3">
              <button 
                onClick={() => setShowAiModal(false)}
                className="flex-1 px-4 py-2 rounded-lg bg-slate-700 hover:bg-slate-600 transition-colors"
              >
                Cancel
              </button>
              <button 
                onClick={generateCustomExercise}
                disabled={isAiLoading}
                className="flex-1 px-4 py-2 rounded-lg bg-emerald-600 hover:bg-emerald-500 transition-colors font-bold disabled:opacity-50"
              >
                {isAiLoading ? "Generating..." : "Generate ✨"}
              </button>
            </div>
          </div>
        </div>
      )}

      <header className="max-w-6xl mx-auto mb-8 flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold bg-gradient-to-r from-blue-400 via-emerald-400 to-indigo-400 bg-clip-text text-transparent">
            Senior Tech Artist Simulator
          </h1>
          <p className="text-slate-400 flex items-center gap-2">
            Interview Prep 
            <span className="bg-indigo-500/20 text-indigo-400 px-2 py-0.5 rounded text-[10px] font-bold">AI ENHANCED</span>
          </p>
        </div>
        
        <div className="flex items-center gap-4">
          <button 
            onClick={() => setShowAiModal(true)}
            className="flex items-center gap-2 px-4 py-2 bg-indigo-600/20 text-indigo-300 border border-indigo-500/30 rounded-lg hover:bg-indigo-600/30 transition-all text-sm font-medium"
          >
            <Sparkles className="w-4 h-4" />
            ✨ Custom Task
          </button>
          
          <div className="flex items-center gap-4 bg-slate-800 p-2 rounded-lg border border-slate-700">
            <div className="flex -space-x-1 max-w-[120px] overflow-hidden">
              {exercises.map((ex, idx) => (
                <div 
                  key={ex.id}
                  className={`min-w-[12px] h-3 rounded-full border border-slate-900 ${
                    solvedStatus[ex.id] ? 'bg-emerald-500' : 
                    idx === currentIdx ? 'bg-blue-500 animate-pulse' : 'bg-slate-600'
                  }`}
                />
              ))}
            </div>
            <span className="text-xs font-mono text-slate-400">
              {currentIdx + 1} / {exercises.length}
            </span>
          </div>
        </div>
      </header>

      <main className="max-w-6xl mx-auto grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Left Column: Prompt & Output */}
        <div className="flex flex-col gap-4">
          <div className="bg-slate-800 rounded-xl p-6 border border-slate-700 shadow-xl relative overflow-hidden">
             {/* Sparkle background decoration */}
            <div className="absolute top-0 right-0 p-4 opacity-10 pointer-events-none">
              <BrainCircuit className="w-32 h-32 text-emerald-400" />
            </div>

            <div className="flex items-center gap-2 mb-4 relative">
              <span className={`px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider ${
                exercise.difficulty === 'Hard' ? 'bg-red-500/20 text-red-400' : 
                exercise.difficulty === 'Medium' ? 'bg-amber-500/20 text-amber-400' : 'bg-emerald-500/20 text-emerald-400'
              }`}>
                {exercise.difficulty}
              </span>
              <span className="text-slate-500 text-sm">•</span>
              <span className="text-slate-400 text-sm">{exercise.category}</span>
            </div>
            
            <h2 className="text-xl font-semibold mb-2 relative">{exercise.title}</h2>
            <p className="text-slate-300 leading-relaxed mb-6 relative">
              {exercise.prompt}
            </p>

            <div className="flex flex-wrap gap-2 mb-6 relative">
              <button 
                onClick={getAiHint}
                className="flex items-center gap-1.5 px-3 py-1.5 bg-slate-700 hover:bg-slate-600 rounded-md text-xs font-medium transition-colors"
              >
                <Lightbulb className="w-3.5 h-3.5 text-amber-400" />
                ✨ Get Hint
              </button>
              <button 
                onClick={getSolution}
                className="flex items-center gap-1.5 px-3 py-1.5 bg-slate-700 hover:bg-slate-600 rounded-md text-xs font-medium transition-colors"
              >
                <Wand2 className="w-3.5 h-3.5 text-indigo-400" />
                ✨ AI Solution
              </button>
              <button 
                onClick={async () => {
                   setOutput("✨ Explaining technical concept...");
                   const res = await callGemini("Explain the technical concept behind this specific Unity Task in depth for a Senior Engineer. Focus on hardware implications and engine architecture.", `Task: ${exercise.title}\nCategory: ${exercise.category}`, false);
                   setOutput(res);
                }}
                className="flex items-center gap-1.5 px-3 py-1.5 bg-slate-700 hover:bg-slate-600 rounded-md text-xs font-medium transition-colors"
              >
                <MessageSquareText className="w-3.5 h-3.5 text-emerald-400" />
                ✨ Explain Concept
              </button>
            </div>

            <div className="flex items-center gap-4 border-t border-slate-700 pt-6">
              <button 
                onClick={handlePrev}
                disabled={currentIdx === 0}
                className="p-2 rounded-lg hover:bg-slate-700 disabled:opacity-30 transition-colors"
              >
                <ChevronLeft className="w-5 h-5" />
              </button>
              <button 
                onClick={handleNext}
                disabled={currentIdx === exercises.length - 1}
                className="flex-1 flex items-center justify-center gap-2 bg-slate-700 hover:bg-slate-600 p-2 rounded-lg transition-colors font-medium"
              >
                Next Exercise <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>

          <div className="flex-1 bg-slate-950 rounded-xl p-6 border border-slate-800 shadow-inner overflow-auto max-h-[400px] min-h-[200px]">
            <div className="flex items-center justify-between mb-3 text-slate-500 uppercase text-[10px] font-bold tracking-widest">
              <div className="flex items-center gap-2">
                <Code2 className="w-3 h-3" />
                Feedback Console
              </div>
              {isAiLoading && <div className="animate-pulse text-emerald-400">✨ Processing...</div>}
            </div>
            <div className={`font-mono text-sm whitespace-pre-wrap leading-relaxed ${output.includes("✨") ? "text-emerald-300" : "text-slate-300"}`}>
              {output || "Select an action or check your code..."}
            </div>
          </div>
        </div>

        {/* Right Column: Code Editor */}
        <div className="flex flex-col bg-slate-800 rounded-xl border border-slate-700 shadow-2xl overflow-hidden min-h-[600px]">
          <div className="bg-slate-900/50 px-4 py-3 flex items-center justify-between border-b border-slate-700">
            <div className="flex items-center gap-3">
              <div className="flex gap-1.5">
                <div className="w-3 h-3 rounded-full bg-red-500/50" />
                <div className="w-3 h-3 rounded-full bg-amber-500/50" />
                <div className="w-3 h-3 rounded-full bg-emerald-500/50" />
              </div>
              <span className="text-xs font-mono text-slate-500">Solution.cs</span>
            </div>
            <div className="flex gap-2">
              <button 
                onClick={() => setCode(exercise.initialCode)}
                className="p-1.5 rounded hover:bg-slate-700 text-slate-400"
                title="Reset Code"
              >
                <RefreshCcw className="w-4 h-4" />
              </button>
              <button 
                onClick={checkSolution}
                disabled={isAiLoading}
                className={`flex items-center gap-2 px-6 py-1.5 rounded-lg font-bold text-sm transition-all ${
                  isAiLoading ? 'bg-slate-700 text-slate-500' : 'bg-emerald-600 hover:bg-emerald-500 text-white shadow-lg shadow-emerald-900/20'
                }`}
              >
                {isAiLoading ? <RefreshCcw className="w-4 h-4 animate-spin" /> : <Play className="w-4 h-4 fill-current" />}
                Check Answer
              </button>
            </div>
          </div>
          
          <div className="flex-1 relative">
            <textarea
              value={code}
              onChange={(e) => setCode(e.target.value)}
              className="absolute inset-0 w-full h-full bg-transparent p-6 font-mono text-sm text-emerald-400 focus:outline-none resize-none spellcheck-false leading-6"
              spellCheck="false"
            />
          </div>

          <div className="p-4 bg-slate-900/30 border-t border-slate-700/50 flex items-center justify-between">
            <div className="flex gap-4">
              <button 
                onClick={async () => {
                  setOutput("✨ Starting performance audit...");
                  const system = `Audit the user's Unity code specifically for mobile performance bottlenecks. Mention draw calls, memory allocation, and battery efficiency.`;
                  const result = await callGemini(system, code, false);
                  setOutput(result);
                }}
                className="flex items-center gap-2 text-indigo-400 hover:text-indigo-300 text-xs font-semibold group"
              >
                <Zap className="w-3 h-3 group-hover:animate-pulse" />
                ✨ Performance Audit
              </button>
            </div>
            {solvedStatus[exercise.id] && (
              <div className="flex items-center gap-1 text-emerald-400 text-xs font-bold uppercase">
                <CheckCircle2 className="w-3 h-3" />
                Solved
              </div>
            )}
          </div>
        </div>
      </main>

      <footer className="max-w-6xl mx-auto mt-12 text-center text-slate-500 text-xs flex flex-col items-center gap-2 pb-12">
         <div className="flex gap-4">
           <span>Memory Management</span>
           <span>•</span>
           <span>Vector Arithmetic</span>
           <span>•</span>
           <span>Shader Pipeline</span>
         </div>
         <p>© {new Date().getFullYear()} Tech Artist Interview AI Coach</p>
      </footer>
    </div>
  );
}