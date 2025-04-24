/* 
from abc import ABC, abstractmethod
from typing import Union, AsyncIterable, List
from common.types import Task
from common.types import (
    JSONRPCResponse,
    TaskIdParams,
    TaskQueryParams,
    GetTaskRequest,
    TaskNotFoundError,
    SendTaskRequest,
    CancelTaskRequest,
    TaskNotCancelableError,
    SetTaskPushNotificationRequest,
    GetTaskPushNotificationRequest,
    GetTaskResponse,
    CancelTaskResponse,
    SendTaskResponse,
    SetTaskPushNotificationResponse,
    GetTaskPushNotificationResponse,
    PushNotificationNotSupportedError,
    TaskSendParams,
    TaskStatus,
    TaskState,
    TaskResubscriptionRequest,
    SendTaskStreamingRequest,
    SendTaskStreamingResponse,
    Artifact,
    PushNotificationConfig,
    TaskStatusUpdateEvent,
    JSONRPCError,
    TaskPushNotificationConfig,
    InternalError,
)
from common.server.utils import new_not_implemented_error
import asyncio
import logging

logger = logging.getLogger(__name__)

class TaskManager(ABC):
    @abstractmethod
    async def on_get_task(self, request: GetTaskRequest) -> GetTaskResponse:
        pass

    @abstractmethod
    async def on_cancel_task(self, request: CancelTaskRequest) -> CancelTaskResponse:
        pass

    @abstractmethod
    async def on_send_task(self, request: SendTaskRequest) -> SendTaskResponse:
        pass

    @abstractmethod
    async def on_send_task_subscribe(
        self, request: SendTaskStreamingRequest
    ) -> Union[AsyncIterable[SendTaskStreamingResponse], JSONRPCResponse]:
        pass

    @abstractmethod
    async def on_set_task_push_notification(
        self, request: SetTaskPushNotificationRequest
    ) -> SetTaskPushNotificationResponse:
        pass

    @abstractmethod
    async def on_get_task_push_notification(
        self, request: GetTaskPushNotificationRequest
    ) -> GetTaskPushNotificationResponse:
        pass

    @abstractmethod
    async def on_resubscribe_to_task(
        self, request: TaskResubscriptionRequest
    ) -> Union[AsyncIterable[SendTaskResponse], JSONRPCResponse]:
        pass

*/

// Translate the above Python to C# code
using System.Security.Cryptography.X509Certificates;

namespace A2ALib;

public class WorkerAgent 
{
    private readonly TaskManager _taskManager;

    private int _taskIdCounter = 0;
    private Dictionary<string, AgentState> _AgentStates = new Dictionary<string, AgentState>();

    private enum AgentState
    {
        Planning,
        WaitingForFeedbackOnPlan,
        Researching
    }
    public WorkerAgent(TaskManager taskManager)
    {
        _taskManager = taskManager;
        _taskManager.OnTaskCreated = async (task) => {
            _AgentStates[task.Id] = AgentState.Planning;
            var message = ((TextPart)task.History.Last().Parts[0]).Text;
            await Invoke(task.Id, message);
         };
         _taskManager.OnTaskUpdated = async (task) => {
            var message = ((TextPart)task.History.Last().Parts[0]).Text;
            await Invoke(task.Id, message);
         };
    }

    public async Task Invoke(string taskId, string message) {

        switch (_AgentStates[taskId])
        {
            case AgentState.Planning:
                await DoPlanning(taskId, message);
                break;
            case AgentState.WaitingForFeedbackOnPlan:
                if (message == "go ahead")
                {
                    await DoResearch(taskId, message);
                }
                else
                {
                    await _taskManager.UpdateStatus(taskId, TaskState.InputRequired, new Message()
                    {
                        Parts = [new TextPart() { Text = "When ready say go ahead" }],
                    });
                }
                break;
            case AgentState.Researching:
                await DoResearch(taskId, message);
                break;
        }
    }

    private async Task DoResearch(string taskId, string message)
    {

        await _taskManager.UpdateStatus(taskId, TaskState.Working);

        await _taskManager.ReturnArtifact(
            new TaskIdParams() { Id = taskId },
            new Artifact()
            {
                Parts = [new TextPart() { Text = $"{message} received." }],
            });

        await _taskManager.UpdateStatus(taskId, TaskState.Completed, new Message()
        {
            Parts = [new TextPart() { Text = "Task completed successfully" }],
        });
    }

    private async Task DoPlanning(string taskId, string message)
    {
        // Task should be in status Submitted
        // Simulate being in a queue for a while
        await Task.Delay(1000);
        // Simulate processing the task
        await _taskManager.UpdateStatus(taskId, TaskState.Working);

        await _taskManager.ReturnArtifact(
            new TaskIdParams() { Id = taskId },
            new Artifact()
            {
                Parts = [new TextPart() { Text = $"{message} received. Task # {_taskIdCounter}" }],
            });

        await _taskManager.UpdateStatus(taskId, TaskState.InputRequired, new Message()
        {
            Parts = [new TextPart() { Text = "When ready say go ahead" }],
        });
        _AgentStates[taskId] = AgentState.WaitingForFeedbackOnPlan;
    }
}