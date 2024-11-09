import * as streams from 'web-streams-polyfill/ponyfill'; // Stream polyfill
import crypto from 'crypto-browserify'; // Crypto polyfill

if (typeof globalThis.crypto === 'undefined') {
  globalThis.crypto = {
    getRandomValues: (arr) => {
      for (let i = 0; i < arr.length; i++) arr[i] = Math.floor(Math.random() * 256);
      return arr;
    },
  };
}

(globalThis as any).ReadableStream = streams.ReadableStream;
(globalThis as any).WritableStream = streams.WritableStream;

import { StateGraph, START, END, Annotation } from '@langchain/langgraph/web';
import { BaseMessage, HumanMessage } from '@langchain/core/messages';

// LangGraph Setup
const GraphState = Annotation.Root({
  messages: Annotation<BaseMessage[]>({ reducer: (x, y) => x.concat(y) }),
});

// Function to process user input dynamically
export async function processUserInput(userInputMessage: string) {
  console.log("Processing User Input:", userInputMessage);

  const nodeFn = async (_state: any) => {
    // Ensure the message is dynamically injected here
    return { messages: [new HumanMessage(userInputMessage)] };
  };

  const workflow = new StateGraph(GraphState)
    .addNode('node', nodeFn)
    .addEdge(START, 'node')
    .addEdge('node', END);

  const app = workflow.compile({});

  try {
    const initialState = { messages: [{ content: userInputMessage }] };
    const finalState = await app.invoke(initialState);

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

// Ensure polyfills and environment are initialized
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

// Initialize environment
initializeEnvironment();
