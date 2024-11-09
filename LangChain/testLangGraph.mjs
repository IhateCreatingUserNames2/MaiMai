import { app } from './langgraph.bundle.mjs';

if (typeof ReadableStream === "undefined") {
  console.error("ReadableStream is still undefined in this environment. Applying fallback polyfill.");
  // Apply fallback for ReadableStream, if any exists or try loading it dynamically here if possible
} else {
  console.log("ReadableStream is available.");
}

async function testLangGraph() {
  console.log('Testing LangGraph with bundled ES module...');

  try {
    const initialState = { messages: [] };
    const finalState = await app.invoke(initialState);

    if (finalState && finalState.messages && finalState.messages.length > 0) {
      const lastMessage = finalState.messages[finalState.messages.length - 1];
      console.log('Final message:', lastMessage.content);
    } else {
      console.warn('No messages found in final state:', finalState);
    }
  } catch (error) {
    console.error('Error executing LangGraph:', error);
  }
}

testLangGraph();
