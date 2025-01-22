import * as streams from 'web-streams-polyfill/ponyfill';
import { StateGraph, START, END, Annotation } from '@langchain/langgraph/web';
import { BaseMessage, HumanMessage } from '@langchain/core/messages';

// Polyfill environment
if (typeof globalThis.crypto === 'undefined') {
  globalThis.crypto = {
    getRandomValues: (arr) => {
      for (let i = 0; i < arr.length; i++) {
        arr[i] = Math.floor(Math.random() * 256);
      }
      return arr;
    },
  };
}

async function initializeEnvironment() {
  if (typeof ReadableStream === 'undefined') {
    console.error('ReadableStream is undefined. Applying fallback polyfill.');
    const streams = await import('web-streams-polyfill/ponyfill');
    globalThis.ReadableStream = streams.ReadableStream;
    globalThis.WritableStream = streams.WritableStream;
    globalThis.TransformStream = streams.TransformStream;
  } else {
    console.log('ReadableStream is available.');
  }
}

// Graph State
const GraphState = Annotation.Root({
  messages: Annotation<BaseMessage[]>({ reducer: (x, y) => x.concat(y) }),
  agentData: Annotation<any>(),
  chainOfThought: Annotation<string>({ reducer: (_, newVal) => newVal }),
  chosenTools: Annotation<string[]>({ reducer: (_, newVal) => newVal }),
  chosenAnimation: Annotation<string>({ reducer: (_, newVal) => newVal }),
  debug: Annotation<boolean>({ reducer: (_, newVal) => newVal }),
});

// Response Cache
const responseCache: Map<string, string> = new Map();

function requestLLMCompletionPromise(agentId: string, prompt: string, stopSequences: string[] = []): Promise<string> {
  if (responseCache.has(prompt)) {
    console.log(`Cache hit for prompt: ${prompt}`);
    return Promise.resolve(responseCache.get(prompt)!);
  }

  return new Promise((resolve, reject) => {
    if (!stopSequences || stopSequences.length === 0) {
      stopSequences = ['\n', 'END']; // Default stop sequences
    }

    CS?.InitializePuerts?.Instance?.RequestLLMCompletionWithCallback(agentId, prompt, stopSequences, (result: string) => {
      responseCache.set(prompt, result);
      resolve(result);
    });
  });
}

function extractSingleJSON(rawOutput: string): { chainOfThought: string; chosenTools: string[]; chosenAnimation: string } {
  if (!rawOutput) return { chainOfThought: '', chosenTools: [], chosenAnimation: '' };

  const sanitized = rawOutput.replace(/```json[\s\S]*?```|```[\s\S]*?```/g, '').trim();
  const matches = sanitized.match(/\{[\s\S]*?\}/g);
  if (!matches) return { chainOfThought: '', chosenTools: [], chosenAnimation: '' };

  for (const match of matches) {
    try {
      const parsed = JSON.parse(match);
      if (typeof parsed.chainOfThought === 'string' && Array.isArray(parsed.chosenTools) && typeof parsed.chosenAnimation === 'string') {
        return parsed;
      }
    } catch {
      // Ignore parse errors
    }
  }

  return { chainOfThought: '', chosenTools: [], chosenAnimation: '' };
}

// Workflow
const workflow = new StateGraph(GraphState)
  .addNode('analyzeIntent', async (state) => {
    const lastUserMessage = state.messages[state.messages.length - 1]?.content || '';
    const agentId = state.agentData?.id;

    const prompt = `
You are an intent parser. Your task is to analyze the user's input and determine their intent.

USER INPUT:
"${lastUserMessage}"

INSTRUCTIONS:
- Output exactly ONE JSON object with the following structure:
{
  "chainOfThought": "...",
  "chosenTools": [...],
  "chosenAnimation": "..."
}

Respond ONLY with the JSON object.`;

    let chainOfThought = '';
    let chosenTools: string[] = [];
    let chosenAnimation = '';

    try {
      const rawOutput = await requestLLMCompletionPromise(agentId, prompt);
      console.log('Raw LLM output (intent):', rawOutput);
      const result = extractSingleJSON(rawOutput);

      chainOfThought = result.chainOfThought;
      chosenTools = result.chosenTools.length > 0 ? result.chosenTools : ['CHAT'];
      chosenAnimation = result.chosenAnimation || '';
    } catch (error) {
      console.error('Error in analyzeIntent:', error);
      chainOfThought = 'Failed to parse chain of thought.';
      chosenTools = ['CHAT'];
      chosenAnimation = '';
    }

    return { ...state, chainOfThought, chosenTools, chosenAnimation };
  })
  .addEdge(START, 'analyzeIntent')
  .addNode('executeOutcome', async (state) => {
    const { agentData, chosenTools, chainOfThought, chosenAnimation, debug } = state;
    const agentId = agentData?.id;

    if (debug) console.debug(`(Debug) chainOfThought: ${chainOfThought}`);

    if (chosenTools.includes('PLAY_ANIMATION') && chosenAnimation) {
      CS?.AINPCTools?.PlayAnimation(agentId, chosenAnimation);
    }

    if (chosenTools.includes('CHAT')) {
      const lastUserMessage = state.messages[state.messages.length - 1]?.content || '';
      const responsePrompt = `
User: "${lastUserMessage}"
(Internal chainOfThought: ${chainOfThought} -- do NOT reveal it.)
Respond concisely.`;

      try {
        const response = await requestLLMCompletionPromise(agentId, responsePrompt);
        console.log('LLM Chat Response:', response);
        state.messages.push(new HumanMessage(response.trim()));
      } catch (error) {
        console.error('Error in executeOutcome:', error);
        state.messages.push(new HumanMessage('I couldn’t process your request, sorry.'));
      }
    }

    return { ...state };
  })
  .addEdge('analyzeIntent', 'executeOutcome')
  .addEdge('executeOutcome', END);

// Compile the graph
const app = workflow.compile({});

// Process User Input
export async function processUserInput(inputData: string) {
  const { userInput, agentId, agentName } = JSON.parse(inputData);
  if (!userInput || !agentId) {
    console.warn('Invalid input: userInput or agentId is missing.');
    CS?.InitializePuerts?.Instance?.ReportError('Invalid input');
    return;
  }

  const initialState = {
    messages: [{ content: userInput }],
    agentData: { id: agentId, name: agentName || 'Unknown Agent' },
    chainOfThought: '',
    chosenTools: [],
    chosenAnimation: '',
    debug: false,
  };

  try {
    const finalState = await app.invoke(initialState);
    const lastMessage = finalState.messages?.[finalState.messages.length - 1];
    if (lastMessage) {
      console.log('LangGraph Final Message:', lastMessage.content);
      CS?.InitializePuerts?.Instance?.SendMessageToLLMCharacter(lastMessage.content, agentId);
    }
  } catch (error) {
    console.error('Error during LangGraph processing:', error);
    CS?.InitializePuerts?.Instance?.ReportError(error.message);
  }
}

// Initialize environment
initializeEnvironment();
