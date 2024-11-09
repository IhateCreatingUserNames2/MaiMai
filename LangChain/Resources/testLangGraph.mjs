import { app } from './langgraph.bundle.mjs';  // Import the bundled LangGraph app

// Ensure the necessary polyfills are applied
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

// Function to process user input
export async function processUserInput(userInputMessage) {
  console.log("Processing User Input:", userInputMessage);

  try {
    const initialState = { messages: [{ content: userInputMessage }] };
    console.log("Initial state:", initialState); // Debugging log

    const finalState = await app.invoke(initialState);
    console.log("Final state:", finalState); // Debugging log

    if (finalState && finalState.messages && finalState.messages.length > 0) {
      const lastMessage = finalState.messages[finalState.messages.length - 1];
      console.log("LangGraph Final Message:", lastMessage.content);

      // Send message to LLMCharacter via Puerts
      if (CS.MFPS.Scripts.InitializePuerts.Instance) {
        CS.MFPS.Scripts.InitializePuerts.Instance.SendMessageToLLMCharacter(lastMessage.content);
      } else {
        console.error("Error: Puerts integration not found");
      }
    } else {
      console.warn("No messages found in LangGraph final state:", finalState);
    }
  } catch (error) {
    console.error("Error during LangGraph and LLMUnity test:", error);
  }
}

initializeEnvironment();


initializeEnvironment();
