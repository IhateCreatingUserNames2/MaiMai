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

// Define possible valid tools/actions explicitly
const VALID_ACTIONS = [
  "ATTACK",
  "DEFEND",
  "CAST_SPELL",
  "HEAL",
  "RETREAT",
  "PLAY_ANIMATION"
];

// Graph State Definition
const GraphState = Annotation.Root({
  messages: Annotation<BaseMessage[]>({ reducer: (x, y) => x.concat(y) }),
  agentData: Annotation<any>(),
  chosenAction: Annotation<string>({ reducer: (_, newVal) => newVal }),
  chosenAnimation: Annotation<string>({ reducer: (_, newVal) => newVal }),
});

// LLM completion request wrapper
async function requestLLM(agentId: string, prompt: string): Promise<string> {
  return new Promise((resolve) => {
    CS?.InitializePuerts?.Instance?.RequestLLMCompletionWithCallback(
      agentId, prompt, ["\n", "END"],
      (result: string) => resolve(result)
    );
  });
}

// Extract a valid JSON object from LLM raw output
function parseLLMOutput(raw: string): { chosenAction: string, chosenAnimation: string } {
  try {
    const match = raw.match(/\{[\s\S]*?\}/);
    if (!match) return { chosenAction: "", chosenAnimation: "" };
    const parsed = JSON.parse(match[0]);
    const chosenAction = VALID_ACTIONS.includes(parsed.chosenAction) ? parsed.chosenAction : "";
    const chosenAnimation = typeof parsed.chosenAnimation === 'string' ? parsed.chosenAnimation : "";
    return { chosenAction, chosenAnimation };
  } catch {
    return { chosenAction: "", chosenAnimation: "" };
  }
}

// Workflow definition
const workflow = new StateGraph(GraphState)
  .addNode("parseIntent", async (state) => {
    const input = state.messages[state.messages.length - 1].content;
    const agentId = state.agentData.id;

    const llmPrompt = `
You control a soldier in a turn-based battle. Decide what action matches user's input EXACTLY from this list:

${VALID_ACTIONS.join(", ")}

If the user's input doesn't CLEARLY match an action, return empty "".

Respond ONLY with JSON:
{
  "chosenAction": "...",
  "chosenAnimation": "..." // optional animation name or empty ""
}

User Input: "${input}"
`;

    const rawOutput = await requestLLM(agentId, llmPrompt);
    console.log("LLM Raw Output:", rawOutput);
    const { chosenAction, chosenAnimation } = parseLLMOutput(rawOutput);

    return { ...state, chosenAction, chosenAnimation };
  })
  .addEdge(START, "parseIntent")

  .addNode("executeAction", async (state) => {
    const { agentData, chosenAction, chosenAnimation } = state;
    const agentId = agentData.id;

    if (!chosenAction) {
      console.log("No valid action found. Skipping execution.");
      return state;
    }

    // Execute relevant function based on chosenAction
    switch (chosenAction) {
      case "ATTACK":
        CS?.AINPCTools?.AttackEnemy(agentId);
        break;
      case "DEFEND":
        CS?.AINPCTools?.DefendPosition(agentId);
        break;
      case "CAST_SPELL":
        CS?.AINPCTools?.CastSpell(agentId);
        break;
      case "HEAL":
        CS?.AINPCTools?.HealSelf(agentId);
        break;
      case "RETREAT":
        CS?.AINPCTools?.Retreat(agentId);
        break;
      case "PLAY_ANIMATION":
        if (chosenAnimation) {
          CS?.AINPCTools?.PlayAnimation(agentId, chosenAnimation);
        }
        break;
      default:
        console.log("Action unmatched. No execution.");
    }

    return state;
  })
  .addEdge("parseIntent", "executeAction")
  .addEdge("executeAction", END);

// Compile workflow
const app = workflow.compile({});

// Entry Point to Process User Input
export async function processUserInput(inputData: string) {
  const { userInput, agentId, agentName } = JSON.parse(inputData);

  if (!userInput || !agentId) {
    CS?.InitializePuerts?.Instance?.ReportError("Invalid input");
    return;
  }

  const initialState = {
    messages: [{ content: userInput }],
    agentData: { id: agentId, name: agentName || "Soldier" },
    chosenAction: "",
    chosenAnimation: "",
  };

  try {
    await app.invoke(initialState);
    console.log(`Processed input for agent ${agentName}`);
  } catch (error) {
    console.error("LangGraph Error:", error);
    CS?.InitializePuerts?.Instance?.ReportError(error.message);
  }
}

// Initialize
initializeEnvironment();
