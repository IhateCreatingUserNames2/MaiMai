import { app } from './langgraph.bundle.mjs'; // Import the bundled LangGraph app

// Polyfill for ReadableStream and related features
async function initializeEnvironment() {
  if (typeof ReadableStream === "undefined") {
    console.error("ReadableStream is undefined. Applying fallback polyfill.");
    const streams = await import('web-streams-polyfill/ponyfill');
    globalThis.ReadableStream = streams.ReadableStream;
    globalThis.WritableStream = streams.WritableStream;
    globalThis.TransformStream = streams.TransformStream;
    globalThis.ByteLengthQueuingStrategy = streams.ByteLengthQueuingStrategy;
    globalThis.CountQueuingStrategy = streams.CountQueuingStrategy;
  } else {
    console.log("ReadableStream is available.");
  }
}

// Process user input and interact with LangGraph
export async function processUserInput(inputData) {
  const { userInput, agentId, agentName } = JSON.parse(inputData);

  if (!userInput || !agentId) {
    console.warn("Invalid input: User input or agent ID is missing.");
    return;
  }

  console.log("Processing User Input:", userInput, "for agent ID:", agentId);

  try {
    const agentNameToUse = agentName || "Unknown Agent";
    const initialState = {
      messages: [{ content: userInput }],
      agentData: agentNameToUse,
    };

    console.log("Initial state:", JSON.stringify(initialState, null, 2));

    // Invoke LangGraph with the initial state
    const finalState = await app.invoke(initialState);
    if (finalState && finalState.messages?.length > 0) {
      const lastMessage = finalState.messages[finalState.messages.length - 1];
      console.log("LangGraph Final Message:", lastMessage.content);
    } else {
      console.warn("LangGraph returned no messages in the final state:", finalState);
    }
  } catch (error) {
    console.error("Error during LangGraph interaction:", error);
  }
}

// Initialize environment when the module is loaded
initializeEnvironment();
