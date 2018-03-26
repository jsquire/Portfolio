# Order Fulfillment #

### Overview ###

Originally created in 2016 as a proof-of-concept, this project is an example of an asynchronous order processing workflow built on Microsoft Azure. It consists of an API for requesting fulfillment of an order placed in an external eCommerce system and associated wofkflow processors to fulfill that order by taking the steps needed to submit it for production.  

Once the user interactions are complete, the eCommerce platform has the responsibility of initiating order fulfillment via a web hook, implemented by the API.  Once the fulfillment request has been received and acknowledged, an asynchonous workflow takes responsibility for the needed steps to prepare the order and request that it be produced, interacting with external services as needed.  

The workflow was designed to be self-aware and self-healing, offering a high level of resiliency by using multiple levels of back off to counter transient failures and longer-term outages.  It also has the ability to resume work at safe checkpoints by making use of message passing across discrete steps of the workflow.  When automatic recovery is not possible, it seeks help from humans by sending alerts via email.


### Known Issues ###

While the tests are self-contained and can be run both locally and on a build server, doing any local development or debugging does require deploying services to Azure and setting local configuration to make use of them.

In addition, though the initial prototype code has been cleaned up and been mostly polished to production-level standards, there exist some areas, particularly in the data models, where formatting and commenting was left in a bit rougher state as they are but examples for illustration that don't actually correspond to any real-world system.  There seemed little benefit in creating detailed documentation for their "use" in the external systems.


