/**
  * This script serves as a shim between the PhantomJS browser and Jasmine unit testing library, allowing
  * Jasmine-based test suits to be executed.
  *
  * This script was adapted from the example Jasmine runner (./examples/run-jasmine.js) included in the PhantomJS
  * download package.
  *
  * @param {object} require    The intrinsic PhantomJS object responsible for including Phantom components
  * @param {object} phantom    The intrinsic PhantomJS instance
  * @param {object} document   The intrinsic PhantomJS document in the current scope
  * @param {object} undefined  A placeholder for an undefined value; it is intended that this parameter not be passed so that it assumes an undefined value
*/
(function(require, phantom, document, undefined)
{  
  //  ==========================  
  // | Static Value Definitions | 
  //  ==========================  

  var configuration =
  {
    // The number of expected arguments passed on the command line.
    expectedArguments : 2,

    // The index for the location of the test suite location in the command line arguments.
    testSuiteLocationArgumentIndex : 2,

    // The number of seconds to allow the test run to complete.  If the suite takes longer to run, it will be aborted.
    timeoutSeconds : 60,

    // The number of milliseconds that should be used for the delay in polling whether a test run has completed or not.
    testRunPollIntervalMilliseconds : 100
  };

  var selectors = 
  {
    // The element of the Jasmine results that displays the summary text.
    summaryArea : '.symbol-summary',

    // The element of the Jasmine results whose presence indicates that the tests are running.
    pendingIndicator : '.symbol-summary .pending',

    // The element of the Jasmine results whose presence indicates that the tests have passed.
    passedIndicator : '.alert > .passed.bar',

    // The element of the Jasmine results whose presence indicates that tests have been skipped.
    skippedIndicator : '.alert > .bar.skipped',

    // The element of the Jasmine results that contains the failure specifics.
    failureContainer : '.results > .failures > .spec-detail.failed',

    // The Jasmine failure result element that contains the description of the failure.
    failureDescription : '.description',

    // The Jasmine failure result element that contains the message associated with the failure.
    failureMessage : '.result-message'
  };

  var colorCodes =
  {
    error    : '[[|Red|]]',
    warning  : '[[|Yellow|]]',
    emphasis : '[[|Green|]]'
  };

  var messages =
  {
    indent              : '    ',
    commandLineUsage    : 'Usage: phantomjs.exe jasmine-runner.js {test suite URL}',
    testFailureSummary  : ' test(s) FAILED:',
    unknownStatus       : 'Unknown Test State',
    timeout             : 'The script timed out after ',
    timeoutUnit         : ' seconds',
    unableToOpen        : 'Unable to open ',
    statusCodeReturn    : 'Returned status code was: ',
    runningSuiteSummary : 'Running test suite: ',
    suiteLocation       : 'Test suite location: '
  };

  var exitCodes =
  {
    normal       : 0,
    failure      : 1,
    unknownState : 2
  };

  //  ===========================  
  // | Phantom Environment Setup | 
  //  ===========================  

  var system = require('system');
  var page   = require('webpage').create();

  //  ======================  
  // | Function Definitions | 
  //  ======================  

  /**
    * Ensures that a target value is defined and is non-null.  If not, then
    * an exception is thrown.
    *
    * @param {object} target  The target object to consider
    * @param {string} name    If populated, the name to include in the exception context to help identify what was not defined
  */
  function ensureDefined(target, name)
  {
    if ((target === undefined) || (target === null))
    {
      if ((name !== undefined) && (name !== null))
      {
        throw name + ' is not a valid value';
      }
      else
      {
        throw 'Invalid argument';
      }
    }
  }

  /**
    * Determines if an HTTP response code is considered a success
    *
    * @param {string} statusCode  The HTTP response status code to consider
    *
    * @returns {bool}  true if the status code was successful; otherwise, false.
  */
  function isSuccessfulHttpResponse(statusCode)
  {
    if ((statusCode === undefined) || (statusCode === null))
    {
      return false;
    }

    if (typeof(statusCode !== 'string'))
    {
      statusCode = statusCode.toString();
    }

    // All HTTP status codes should be at least 3 characters long.

    if (statusCode.length < 3)
    {
      return false;
    }

    // If the status code is a 1xx, 2xx, or 3xx series code, then consider it
    // a successful response.

    return (parseInt(statusCode.substr(0, 1), 10) <= 3);
  }
  
  /**
    * Determines if the test run has completed.
    *
    * @param {object} page       The PhantomJS page that is hosting the test run 
    * @param {object} selectors  The set of selectors to use for test suite page inspection
    *
    * @returns {bool} true if the test suite has completed; otherwise, false
  */
  function isTestRunComplete(page, selectors)
  {   
    ensureDefined(selectors, 'selectors');

    // If there is no page, there is no test suite running.

    if ((page === undefined) || (page === null))
    {
      return true;
    }

    return page.evaluate(function(selectors)
    {
      return ((document.querySelector(selectors.summaryArea) !== null) && (document.querySelector(selectors.pendingIndicator) === null));

    }, selectors);
  }
  
  /**
    * Parses the results of a test suite run.
    *    
    * @param {object} page        The PhantomJS page that is hosting the test run 
    * @param {object} selectors   The set of selectors to use for test suite page inspection
    * @param {object} colorCodes  The set of color codes to be combined with message text for console output
    * @param {object} messages    The set of message text to be written to the console
    * @param {object} exitCodes   The set of exit codes to use for return values
    *
    * @returns {object} The outcome of parsing the test run results.  The return structure will be:
    * {
    *   {int}    exitCode  The exit code that should be set when terminating the PhantomJS process
    *   {string} status    A message that describes the overall status of the test run
    *   {array}  failures  The set of failure objects, if any, containing string members for description and result
    * }    
  */
  function parseTestRun(page, selectors, messages, exitCodes)
  {    
    ensureDefined(page, 'page');
    ensureDefined(selectors, 'selectors');
    ensureDefined(messages, 'messages');
    ensureDefined(exitCodes, 'exitCodes');
    
    return page.evaluate(function(messages, selectors, exitCodes)
    {   
      var result =
      {
        exitCode : exitCodes.unknownState,
        status   : messages.unknownStatus,
        failures : []
      };

      var failures = document.querySelectorAll(selectors.failureContainer);

      // If there were failures located, then update the results

      if ((failures !== undefined) && (failures !== null) && (failures.length > 0))
      {
        result.exitCode = exitCodes.failures;
        result.status   = failures.length + messages.testFailureSummary
    
        for (var index = 0; index < failures.length; ++index)
        {
          result.failures.push(
          {
            description : failures[index].querySelector(selectors.failureDescription).innerText,
            result      : failures[index].querySelector(selectors.failureMessage).innerText
          });  
        }
      }
       else
       {
        // No tests failed, attempt to update the status text.

        var statusSource = document.querySelector(selectors.passedIndicator);

        if ((statusSource === undefined) || (statusSource === null))
        {
          statusSource = document.querySelector(selectors.skippedIndicator);
        }

        if ((statusSource !== undefined) && (statusSource !== null))
        {
          result.exitCode = exitCodes.normal;
          result.status   = statusSource.innerText;
        }
      }

      return result;

    }, messages, selectors, exitCodes);
  }

  /**
    * Performs the necessary tasks to wait for a test run to complete, or to
    * determine if the test run has taken too long and timed out.
    *
    * @param {int}      testRunPollIntervalMilliseconds  The number of milliseconds that should be used for the delay in polling whether a test run has completed or not.  If not provided, a default of 100 milliseconds will be used
    * @param {int}      timeoutMilliseconds              The number of milliseconds, at maximum, to allow the test suite to run before timing out.  If not provided, a default of 60 seconds will be used
    * @param {function} testCompleteFunction             The function to invoke to determine if the test run is complete.  Expected signature: {bool} function()
    * @param {function} completedCallback                The function to invoke when the test run has completed or timed out.  Expected signature: function({bool} timedOut)
  */
  function awaitTestRun(testRunPollIntervalMilliseconds, timeoutMilliseconds, testCompleteFunction, completedCallback)
  {    
    if (typeof(completedCallback) !== 'function')
    {
      throw 'Completed callback must be a valid function.  Expected signature: function({bool} timedOut) {}'
    }

    if (typeof(testCompleteFunction) !== 'function')
    {
      throw 'The function to determine test completion must be a valid function.  Expected signature: {bool} function() {}'
    }

    // If no timeout or polling values were provided, then set the default(s).

    if (typeof(testRunPollIntervalMilliseconds) !== 'number')
    {
      testRunPollIntervalMilliseconds = 100;
    }

    if (typeof(timeoutMilliseconds) !== 'number')
    {
      timeoutMilliseconds = (60 * 1000);
    }

    var startTime = new Date().getTime();

    var intervalId = setInterval(function()
    {
      var elapsed  = (new Date().getTime() - startTime);
      var timedOut = null;

      if (testCompleteFunction())
      {
        timedOut = false;
      }
      else if (elapsed > timeoutMilliseconds)
      {
        timedOut = true;
      }

      if (timedOut !== null)
      {
        clearInterval(intervalId);
        completedCallback(timedOut);
        return;
      }

    }, testRunPollIntervalMilliseconds);
  }

  //  ================  
  // | Script Actions | 
  //  ================

  // Validate the command line arguments passed were consistent with
  // expectations.

  if (system.args.length != configuration.expectedArguments)
  {
    console.log(messages.commandLineUsage);
    phantom.exit(exitCodes.failure);
  }
  
  // Perform the actions needed to run the test suite and report on its status.  Because the
  // test suite exists as a stand-alone HTML page, it will be loaded asynchronously necessitating
  // the bulk of these actions be performed in the load callback function.

  var testSuiteUrl        = system.args[1];
  var testSuiteStatusCode = null;
  
  // If the PhantomJS page object loads a resource that matches the requested test suite, then
  // capture its status code for inspection.

  page.onResourceReceived = function(resource)
  {
    if ((resource !== undefined) && (resource !== null) && (resource.url == testSuiteUrl))
    {
      testSuiteStatusCode = resource.status;
    }
  };
 
  page.open(testSuiteUrl, function(status)
  {    
    // If the status of the load was not successful, then consider the run a failure.

    if ((status !== 'success') || ((testSuiteStatusCode !== null) && (!isSuccessfulHttpResponse(testSuiteStatusCode))))
    {
      console.log(colorCodes.error + messages.unableToOpen + '"' + testSuiteUrl + '"');
      
      if ((testSuiteStatusCode !== undefined) && (testSuiteStatusCode !== null))
      {
        console.log(colorCodes.error + messages.indent + 'HTTP Response Status: ' + testSuiteStatusCode);
      }

      phantom.exit(exitCodes.failure);
    }

    // The test suite was successfully loaded and is running.  Update the status
    // before waiting for completion.

    var suiteProperties = page.evaluate(function() 
    {
      return { title: document.title, location: window.location.href };
    });

    console.log('');
    console.log(colorCodes.emphasis + messages.runningSuiteSummary + suiteProperties.title);
    console.log(messages.suiteLocation + unescape(suiteProperties.location));
    console.log('');

    // Create the callback functions needed for awaiting test suite completion.  

    var isTestRunFinishedCallback = function()
    {      
      return isTestRunComplete(page, selectors);
    };

    var waitCompletedCallback = function(timedOut)
    {
      if (timedOut)
      {
        console.log('');
        console.log(colorCodes.warning + messages.timeout + configuration.timeoutSeconds + messages.timeoutUnit);
        console.log('');
        phantom.exit(exitCodes.failure);
      }

      var statusColor = '';      
      var results     = parseTestRun(page, selectors, messages, exitCodes);
      
      switch (results.exitCode)
      {
        case exitCodes.normal:
          statusColor = colorCodes.emphasis;
          break;

        case exitCodes.failed:
          statusColor = colorCodes.error;
          break;

        default:
          statusColor = colorCodes.warning;
          break;
      }
      
      console.log('');
      console.log(statusColor + results.status);

      for (var index = 0; index < results.failures.length; ++index)
      {
        console.log('');
        console.log(colorCodes.error + messages.indent + results.failures[index].description);
        console.log(colorCodes.error + messages.indent + messages.indent + results.failures[index].result);
        console.log('');
        console.log('');
      }
      
      phantom.exit(results.exitCode);
    };
    
    // Wait for the test run to complete or timeout.  This is an asynchronous operation, so the responsibility
    // for updating the status and terminating the PhantomJS process with the appropriate exit code lies with the
    // completed callback.
   
    awaitTestRun(configuration.testRunPollIntervalMilliseconds, (configuration.timeoutSeconds * 1000), isTestRunFinishedCallback, waitCompletedCallback);
  });

})(require, phantom, document);
