import * as streams from 'web-streams-polyfill/ponyfill';
import { StateGraph, START, END, Annotation } from '@langchain/langgraph/web';
import { BaseMessage, HumanMessage } from '@langchain/core/messages';
import { app } from './langgraph.bundle.mjs';

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

// LangGraph State Definition
const GraphState = Annotation.Root({
  messages: Annotation<BaseMessage[]>({ reducer: (x, y) => x.concat(y) }),
  agentData: Annotation<string>(),
});

// Process user input and interact with LangGraph
export async function processUserInput(inputData: string) {
  const { userInput, agentId, agentName } = JSON.parse(inputData);

  if (!userInput || !agentId) {
    console.warn("Invalid input: User input or agent ID is missing.");
    return;
  }

  console.log("Processing User Input:", userInput, "for agent ID:", agentId);

  try {
    const agentNameToUse = agentName || getAgentName(agentId);
    if (!agentNameToUse) {
      console.error("Agent name could not be resolved.");
      return;
    }

    const initialState = {
      messages: [{ content: userInput }],
      agentData: agentNameToUse,
    };

    console.log("Initial state:", JSON.stringify(initialState, null, 2));

    const finalState = await invokeLangGraph(initialState);
    handleFinalState(finalState, agentId);
  } catch (error) {
    console.error("Error during LangGraph interaction:", error);
  }
}

// Helper to retrieve agent name
function getAgentName(agentId: string): string | null {
  try {
    const agent = CS?.AIManagerBridge?.GetAIAgentById(agentId);
    if (!agent) {
      console.error(`Agent name not found for agent ID ${agentId}.`);
      return null;
    }
    return agent.AgentName;
  } catch (error) {
    console.error("Error retrieving agent name:", error);
    return null;
  }
}

// Invoke LangGraph with the given state
async function invokeLangGraph(initialState: any) {
  const workflow = new StateGraph(GraphState)
    .addNode('node', async (_state) => ({
      messages: [new HumanMessage(initialState.messages[0].content)],
    }))
    .addEdge(START, 'node')
    .addEdge('node', END);

  const app = workflow.compile({});
  return await app.invoke(initialState);
}

// Handle final state from LangGraph
function handleFinalState(finalState: any, agentId: string) {
  if (finalState && finalState.messages?.length > 0) {
    const lastMessage = finalState.messages[finalState.messages.length - 1];
    console.log("LangGraph Final Message:", lastMessage.content);

    if (CS?.InitializePuerts?.Instance) {
      CS.InitializePuerts.Instance.SendMessageToLLMCharacter(lastMessage.content, agentId);
    } else {
      console.error("Error: Puerts integration not found or not initialized.");
    }
  } else {
    console.warn("LangGraph returned no messages in the final state:", finalState);
  }
}

// Initialize environment when the module is loaded
initializeEnvironment();
