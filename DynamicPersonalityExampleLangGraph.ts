// Import LangGraphJS library (assuming it's installed)
import * as streams from 'web-streams-polyfill/ponyfill';
const { LangGraph, Layer, StateGraph, START, END, Annotation } = require('@langchain/langgraph/web');
const { BaseMessage, HumanMessage } = require('@langchain/core/messages');
const { llmcharacter } = require('llmunity');

// Polyfill for crypto if not available
if (typeof globalThis.crypto === 'undefined') {
  globalThis.crypto = {
    getRandomValues: (arr) => {
      for (let i = 0; i < arr.length; i++) arr[i] = Math.floor(Math.random() * 256);
      return arr;
    },
  };
}

// Polyfill for ReadableStream
async function initializeEnvironment() {
  if (typeof ReadableStream === 'undefined') {
    console.error("ReadableStream is undefined. Applying fallback polyfill.");
    globalThis.ReadableStream = streams.ReadableStream;
    globalThis.WritableStream = streams.WritableStream;
    globalThis.TransformStream = streams.TransformStream;
    globalThis.ByteLengthQueuingStrategy = streams.ByteLengthQueuingStrategy;
    globalThis.CountQueuingStrategy = streams.CountQueuingStrategy;
  } else {
    console.log("ReadableStream is available.");
  }
}

// Define the LangGraph state with additional layers and humor levels
const GraphState = Annotation.Root({
  messages: Annotation<BaseMessage[]>({ reducer: (x, y) => x.concat(y) }),
  agentData: Annotation<string>(),
  humorLevel: Annotation<number>({ reducer: (_, humor) => humor }), // Humor level for emotional oscillation
  personalityTraits: Annotation<object>(), // Personality traits with weights and dynamics
});

// Helper function to fetch and update personality traits and behaviors dynamically
async function fetchAndUpdatePersonality(state, trait, context) {
  const query = `Evaluate and update trait: ${trait} in context: ${context}`;
  const result = await llmcharacter.complete({ prompt: query, maxTokens: 100 });
  return result;
}

// Define LangGraph workflow with dynamic personality and mood modulation
async function invokeLangGraph(initialState) {
  const workflow = new StateGraph(GraphState)
    .addNode('PersonalityLayer1', async (state) => {
      const traitData = await fetchAndUpdatePersonality(state, 'analytical', state.sharedContext);
      state.personalityTraits.analytical = traitData; // Update trait dynamically
      const humorAdjustedResponse = `Humor ${state.humorLevel} amplifies analytical depth.`;
      return {
        messages: [new HumanMessage(`Layer1 Response: ${traitData}, ${humorAdjustedResponse}`)],
        data: `Layer1 processed: ${traitData}`,
      };
    })
    .addNode('PersonalityLayer2', async (state) => {
      const traitData = await fetchAndUpdatePersonality(state, 'decisive', state.sharedContext);
      state.personalityTraits.decisive = traitData; // Update trait dynamically
      const humorAdjustedResponse = `Humor ${state.humorLevel} drives decisiveness.`;
      return {
        messages: [new HumanMessage(`Layer2 Decision: ${traitData}, ${humorAdjustedResponse}`)],
        decision: `Layer2 action derived from: ${state.data} and ${traitData}`,
      };
    })
    .addEdge(START, 'PersonalityLayer1')
    .addEdge('PersonalityLayer1', 'PersonalityLayer2')
    .addEdge('PersonalityLayer2', END);

  const app = workflow.compile({});
  return await app.invoke(initialState);
}

// Helper function to perform sentiment analysis
async function analyzeSentiment(input) {
  const sentimentQuery = `Analyze sentiment of the following input: "${input}". Provide a score between -1 (very negative) and 1 (very positive).`;
  const result = await llmcharacter.complete({ prompt: sentimentQuery, maxTokens: 10 });
  const sentimentScore = parseFloat(result); // Assuming the model returns a single score
  return isNaN(sentimentScore) ? 0 : sentimentScore; // Default to neutral if parsing fails
}

// Process user input and interact with LangGraph
export async function processUserInput(userInput) {
  const sentimentScore = await analyzeSentiment(userInput);
  const adjustedHumorLevel = Math.max(0, Math.min(10, 5 + sentimentScore * 5)); // Normalize humor level to [0, 10]

  const initialState = {
    personality1: {
      trait: 'analytical',
      actionPref: 'gather_data',
    },
    personality2: {
      trait: 'decisive',
      actionPref: 'make_decision',
    },
    humorLevel: adjustedHumorLevel, // Dynamically adjusted humor level
    personalityTraits: {
      analytical: { level: 0.8 },
      decisive: { level: 0.7 },
    },
    sharedContext: `User Input: ${userInput}`,
    messages: [],
  };

  try {
    console.log("Processing User Input:", userInput);
    console.log("Sentiment Score:", sentimentScore, "Adjusted Humor Level:", adjustedHumorLevel);
    const finalState = await invokeLangGraph(initialState);
    handleFinalState(finalState);
  } catch (error) {
    console.error("Error during LangGraph interaction:", error);
  }
}


// Handle final state from LangGraph
function handleFinalState(finalState) {
  if (finalState && finalState.messages?.length > 0) {
    const lastMessage = finalState.messages[finalState.messages.length - 1];
    console.log("LangGraph Final Message:", lastMessage.content);
  } else {
    console.warn("LangGraph returned no messages in the final state:", finalState);
  }
}

// Initialize environment
initializeEnvironment();
