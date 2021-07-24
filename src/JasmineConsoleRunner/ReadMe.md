# Jasmine Console Runner

### Summary

[Jasmine](http://jasmine.github.io/2.3/introduction.html "Jasmine") is an open source behavior-driven testing framework for javascript.  It's goal is to allow pure testing of code without reliance on browsers, the DOM, or external frameworks.  Out-of-the-box, Jasmine provides a sample stand-alone HTML page to use for the running of tests.  Because of its stand-alone nature, there is no built-in means to run Jasmine tests from the console.

[PhantomJS](http://phantomjs.org/ "PhantomJS") is a headless web browser intended to be invoked from the console.  It provides no user interface, but instead allows arbitrary javascript to be run from the console.  Many unit testing libraries make use of PhantomJS to provide a console-based means of running tests.

While Jasmine no longer does so as part of its package, it is possible to write a small javascript shim to allow Jasmine tests to run against PhantomJS.  This enables Jasmine to more easily be integrated into build/test automation and some development work flows that are centered on the console.  

My goal was to write a simple script to allow running Jasmine tests from the Windows console using PhantomJS.  In order to more easily distinguish test results at a glance, I wanted to enable the output to be colored.  Because it is platform neutral, PhantomJS does not offer any intrinsic support for setting the console output color in the Windows console.  Unlike Unix-based environments in which the color can be influenced by sending a specific control-character sequence to stdout, Windows requires specific API calls to set the console color.  To work around this limitation, I wrote a small shim for launching PhantomJS which will intercept stdout and parse the output for a control character set to influence the color.  Because my intent was to run this on Windows only, I did not adopt the Unix control characters, instead opting for something more easily recognizable at a glance.

### Structure

* **src**  
  _The container for project source code._
  
* **lib**  
  _The container for external libraries referenced by the project._
  
* **example**  
  _The container for a functional example of using the console runner with a Jasmine test suite._
  
* **dist**  
  _The container for project build output intended for distribution.  When using the console runner as part of another project, this is where one should look for the releases._
