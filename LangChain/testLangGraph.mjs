import { app } from './langgraph.bundle.mjs';

async function initializeEnvironment() {
  if (typeof ReadableStream === "undefined") {
    console.error("ReadableStream is undefined. Applying fallback polyfill.");
  } else {
    console.log("ReadableStream is available.");
  }
}

async function testLangGraphWithLLMUnity() {
  console.log("Testing LangGraph with LLMUnity integration...");

  try {
    const initialState = { messages: [] };
    const finalState = await app.invoke(initialState);

    if (finalState && finalState.messages && finalState.messages.length > 0) {
      const lastMessage = finalState.messages[finalState.messages.length - 1];
      console.log("LangGraph Final Message:", lastMessage.content);

      // Call the public instance method to send the message to LLMCharacter
      CS.MFPS.Scripts.InitializePuerts.Instance.SendMessageToLLMCharacter(lastMessage.content);
    } else {
      console.warn("No messages found in LangGraph final state:", finalState);
    }
  } catch (error) {
    console.error("Error during LangGraph and LLMUnity test:", error);
  }
}

initializeEnvironment();
testLangGraphWithLLMUnity();
