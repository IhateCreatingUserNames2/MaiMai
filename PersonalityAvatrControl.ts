// Import LangGraphJS library (assuming it's installed)
import * as streams from 'web-streams-polyfill/ponyfill';
const { LangGraph, Layer, StateGraph, START, END, Annotation } = require('@langchain/langgraph/web');
const { BaseMessage, HumanMessage, SystemMessage } = require('@langchain/core/messages');
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

// --------------------------------------------------------------------------
// 1. Define the LangGraph state with additional layers, including "avatarState".
// --------------------------------------------------------------------------
const GraphState = Annotation.Root({
  messages: Annotation<BaseMessage[]>({ reducer: (x, y) => x.concat(y) }),
  agentData: Annotation<string>(),
  humorLevel: Annotation<number>({ reducer: (_, humor) => humor }),
  personalityTraits: Annotation<object>(),
  sentimentLog: Annotation<number[]>({ reducer: (log, sentiment) => [...log, sentiment] }),
  lastInteractions: Annotation<string[]>({ reducer: (log, interaction) => [...log.slice(-5), interaction] }),
  
  // This is a new piece of state to hold data about your avatar’s position, orientation, etc.
  // In an actual application, these fields would reflect your real-time game/engine state.
  avatarState: Annotation({
    position: { x: 0, y: 0, z: 0 },
    rotation: { x: 0, y: 0, z: 0 },
    isMoving: false,
    // ... any other relevant state
  }),
});

// --------------------------------------------------------------------------
// 2. Helper functions
// --------------------------------------------------------------------------

// a) Update personality traits
async function fetchAndUpdatePersonality(state, trait, context) {
  const query = `Evaluate and update trait: ${trait} in context: ${context}`;
  const result = await llmcharacter.complete({ prompt: query, maxTokens: 100 });
  return result;
}

// b) Sentiment analysis
async function analyzeSentiment(input, traits, humorLevel, sentimentLog, lastInteractions) {
  const sentimentQuery = `Analyze sentiment of the following input: "${input}". Based on this Personality Trait: ${JSON.stringify(traits)} with the Last Humor Level: ${humorLevel}. Previous Sentiment Log: ${JSON.stringify(sentimentLog)}. Last Interactions: ${JSON.stringify(lastInteractions)}. Provide a score between -1 (very negative) and 1 (very positive).`;
  const result = await llmcharacter.complete({ prompt: sentimentQuery, maxTokens: 10 });
  const sentimentScore = parseFloat(result);
  return isNaN(sentimentScore) ? 0 : sentimentScore;
}

// --------------------------------------------------------------------------
// 3. Example “Control Avatar” Layer
//    This node is where the LLM can decide how to manipulate the avatarState.
// --------------------------------------------------------------------------
async function controlAvatarLayer(state) {
  // We’ll provide the LLM a brief “SystemMessage” style prompt with the 
  // current avatar state and a “command format” we expect it to produce. 
  // In practice, you’d want to define a consistent format: JSON, plain text, etc.
  const systemPrompt = `You are controlling a simple humanoid avatar. 
Current avatar state: ${JSON.stringify(state.avatarState)}.

Use the following commands to move the avatar:
- MOVE <x> <y> <z>
- ROTATE <x> <y> <z>
- STOP

Return only the command. For example:
"MOVE 0 0 10"
or 
"ROTATE 0 90 0"
or 
"STOP"`;

  // Pass the system prompt + any user messages from the state
  const mergedPrompt = [
    new SystemMessage(systemPrompt),
    ...state.messages,
  ]
    .map((m) => (m.content || ''))
    .join('\n');

  // Ask the LLM to produce a command
  const command = await llmcharacter.complete({
    prompt: mergedPrompt,
    maxTokens: 50,
  });

  // Simple parser for the command. In practice, you'd want robust parsing and error handling.
  const [action, x, y, z] = command.trim().split(/\s+/);
  
  switch (action?.toUpperCase()) {
    case 'MOVE':
      state.avatarState.position = {
        x: parseFloat(x) || 0,
        y: parseFloat(y) || 0,
        z: parseFloat(z) || 0,
      };
      state.avatarState.isMoving = true;
      break;
    case 'ROTATE':
      state.avatarState.rotation = {
        x: parseFloat(x) || 0,
        y: parseFloat(y) || 0,
        z: parseFloat(z) || 0,
      };
      break;
    case 'STOP':
      state.avatarState.isMoving = false;
      break;
    default:
      // If the LLM returns something unexpected, we might just log it or ignore it
      console.warn("Unknown command from LLM:", command);
      break;
  }

  return {
    messages: [
      // We can give the user some feedback about what happened
      new HumanMessage(`AvatarControlLayer Command: ${command}`),
    ],
  };
}

// --------------------------------------------------------------------------
// 4. Define LangGraph workflow with personality, sentiment, and a new “controlAvatarLayer” node
// --------------------------------------------------------------------------
async function invokeLangGraph(initialState) {
  const workflow = new StateGraph(GraphState)
    .addNode('PersonalityLayer1', async (state) => {
      const traitData = await fetchAndUpdatePersonality(state, 'analytical', state.sharedContext);
      state.personalityTraits.analytical = traitData;
      const humorAdjustedResponse = `Humor ${state.humorLevel} amplifies analytical depth.`;
      return {
        messages: [new HumanMessage(`Layer1 Response: ${traitData}, ${humorAdjustedResponse}`)],
        data: `Layer1 processed: ${traitData}`,
      };
    })
    .addNode('PersonalityLayer2', async (state) => {
      const traitData = await fetchAndUpdatePersonality(state, 'decisive', state.sharedContext);
      state.personalityTraits.decisive = traitData;
      const humorAdjustedResponse = `Humor ${state.humorLevel} drives decisiveness.`;
      return {
        messages: [new HumanMessage(`Layer2 Decision: ${traitData}, ${humorAdjustedResponse}`)],
        decision: `Layer2 action derived from: ${state.data} and ${traitData}`,
      };
    })
    // Add your new avatar control node
    .addNode('ControlAvatarLayer', controlAvatarLayer)

    // Graph edges
    .addEdge(START, 'PersonalityLayer1')
    .addEdge('PersonalityLayer1', 'PersonalityLayer2')
    .addEdge('PersonalityLayer2', 'ControlAvatarLayer')
    .addEdge('ControlAvatarLayer', END);

  // Compile and run the workflow
  const app = workflow.compile({});
  return await app.invoke(initialState);
}

// --------------------------------------------------------------------------
// 5. Process user input and interact with LangGraph
//    Example usage: user says, “Hey, can we move forward?”
// --------------------------------------------------------------------------
export async function processUserInput(userInput, state) {
  const sentimentScore = await analyzeSentiment(
    userInput,
    state.personalityTraits,
    state.humorLevel,
    state.sentimentLog,
    state.lastInteractions
  );
  const adjustedHumorLevel = Math.max(0, Math.min(10, state.humorLevel + sentimentScore * 5));

  const updatedState = {
    ...state,
    humorLevel: adjustedHumorLevel,
    sharedContext: `User Input: ${userInput}`,
    messages: [new HumanMessage(userInput)], // Put the new user message in the message array
    sentimentLog: state.sentimentLog.concat(sentimentScore),
    lastInteractions: state.lastInteractions.concat(userInput),
  };

  try {
    console.log("Processing User Input:", userInput);
    console.log("Sentiment Score:", sentimentScore, "Adjusted Humor Level:", adjustedHumorLevel);
    const finalState = await invokeLangGraph(updatedState);
    handleFinalState(finalState);
  } catch (error) {
    console.error("Error during LangGraph interaction:", error);
  }
}

// --------------------------------------------------------------------------
// 6. Handle final state from LangGraph
// --------------------------------------------------------------------------
function handleFinalState(finalState) {
  if (finalState && finalState.messages?.length > 0) {
    const lastMessage = finalState.messages[finalState.messages.length - 1];
    console.log("LangGraph Final Message:", lastMessage.content);
    // Here you can check finalState.avatarState to see how it changed and
    // feed it into your actual game/engine for real-time updates.
    console.log("Final Avatar State:", finalState.avatarState);
  } else {
    console.warn("LangGraph returned no messages in the final state:", finalState);
  }
}

// Initialize environment at startup
initializeEnvironment();

// --------------------------------------------------------------------------
// Example usage of your "processUserInput" function
// (You might call this from a UI event or some loop in your app.)
// --------------------------------------------------------------------------

// A sample initialState
const initialState = {
  messages: [],
  agentData: "",
  humorLevel: 5,
  personalityTraits: { analytical: {}, decisive: {} },
  sentimentLog: [],
  lastInteractions: [],
  avatarState: {
    position: { x: 0, y: 0, z: 0 },
    rotation: { x: 0, y: 0, z: 0 },
    isMoving: false,
  },
};

// Just an example call. In a real app, userInput might come from a text box or microphone, etc.
processUserInput(
  "Hey I'm just getting my feet wet with this stuff, but as an art/experiment thing I am going to try and give my LLM entity control over an avatar. I'll probably have a series of commands it can use to translate into real simple 3rd person control of a humanoid rig. Is there an easy way to make an LLM aware of a variable state in real time?",
  initialState
);
