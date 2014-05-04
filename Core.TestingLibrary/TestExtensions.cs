using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Habitat.Core.TestingLibrary
{
    /// <summary>
    /// A container for useful extension methods that supporting testing
    /// </summary>
    /// <remarks>
    /// TODO: don't include this in Habitat.Core.Testinglibrary because it depends on System.Web.Mvc
    /// </remarks>
    public static class TestExtensions
    {
        /// <summary>
        /// Method to set the default task scheduler.
        /// </summary>
        /// <remarks>
        /// Useful for unit tests, but not suitable for production use, as this is a pretty substantial hack.
        /// See http://www.jarrodstormo.com/2012/02/05/unit-testing-when-using-the-tpl/
        /// </remarks>
        public static void SetDefaultScheduler(this TaskScheduler scheduler)
        {
            Type taskSchedulerType = typeof(TaskScheduler);
            FieldInfo defaultTaskSchedulerField = taskSchedulerType.GetField("s_defaultTaskScheduler", BindingFlags.SetField | BindingFlags.Static | BindingFlags.NonPublic);
            if (defaultTaskSchedulerField != null)
                defaultTaskSchedulerField.SetValue(null, scheduler);
        }

        /// <summary>
        /// Method to run an MVC3 async controller method as if it was synchronous
        /// </summary>
        /// <typeparam name="TR">The ActionResult type that should be returned by the Completed method of the async operation when it's finished</typeparam>
        /// <typeparam name="TP">The object type that should be passed to the Completed method of the async operation.</typeparam>
        /// <param name="controller">The async controller</param>
        /// <param name="startAction">The action used to call the Start method of the async operation</param>
        /// <param name="completedAction">The function that takes the end result for the Start method and passes it to the Completed method.</param>
        /// <param name="asyncParameter">The name of the parameter accepted by the Completed method</param>
        /// <returns>The ActionResult for the controller method</returns>
        /// <remarks>
        /// This currently supports controllers whose Completed method accepts a single argument.
        /// Additionally, it is up to the developer to ensure that asyncParameter matches the name of the parameter passed to completedAction.
        /// MVC will use reflection to figure this out and if this is malfunctioning, this test helper won't be able to catch it.
        /// </remarks>
        public static TR InvokeAsyncController<TR, TP>(this AsyncController controller, Action startAction, Func<TP, TR> completedAction, string asyncParameter)
            where TR : ActionResult
        {
            var waitHandle = new AutoResetEvent(false);
            EventHandler eventHandler = (sender, e) => waitHandle.Set();
            controller.AsyncManager.Finished += eventHandler;
            startAction();
            waitHandle.WaitOne();
            TP result = (TP)controller.AsyncManager.Parameters[asyncParameter];
            return completedAction(result); // NOTE: this does not validate the parameter name, which matters when MVC calls this (we're only simulating it here)
        }
    }
}