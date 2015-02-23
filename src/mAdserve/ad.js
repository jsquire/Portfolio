(function(window, document, undefined)
{ 
  //  ===============================
  // |  Inline Component Inclusions  |
  //  ===============================  

  var DOMReady=(function(){var b=[],d=false,a=null,e=function(g,f){try{g.apply(this,f||[])}catch(h){if(a){a.call(this,h)}}},c=function(){d=true;for(var f=0;f<b.length;f++){e(b[f].fn,b[f].args||[])}b=[]};this.setOnError=function(f){a=f;return this};this.add=function(g,f){if(d){e(g,f)}else{b[b.length]={fn:g,args:f}}return this};if(window.addEventListener){document.addEventListener("DOMContentLoaded",function(){c()},false)}else{(function(){if(!document.uniqueID&&document.expando){return}var g=document.createElement("document:ready");try{g.doScroll("left");c()}catch(f){setTimeout(arguments.callee,0)}})()}return this})();

  //  ===============================
  // |  Private Member Declarations  |
  //  ===============================  

  var ELEMENT_NODE = (window.ELEMENT_NODE || 1);
  var TEXT_NODE    = (window.TEXT_NODE || 3);

  var externalScriptMonitor =
  {
    pendingLoads   : 0,
    continuations  : []
  };

  var scriptProxyPath = '/madserve-script-proxy.php?url=';
  var adCount         = 0;

  /**
    * Performs basic screening to ensure that an object is defined 
    * and non-null.
    *
    * @param target {object}  The object to test
    *
    * @returns {boolean}  True if the object is defined; otherwise, false.
  */
  function isDefined(target)
  {
    return ((typeof(target) !== 'undefined') && (target !== null));
  }
 
  /**
    * Determines if a given url target would consititue a cross-domain
    * request.
    *
    * @param targetUrl  {string}  The target url to consider
    *
    * @returns  {boolean}  True if the request would cross domains; otherwise, false
  */
  function isCrossDomain(targetUrl)
  {
    var a = document.createElement('a');
    a.href = targetUrl;
 
    return (a.hostname !== window.location.hostname);
  }

  /**
    * Performs the tasks needed to build the url to the script proxy
    * using the adRequest url as the base.
    *
    * @param adRequestUrl  {string}  The url used to request the ad
    *
    * @returns {string}  
  */
  function buildScriptProxyUrl(adRequestUrl)
  {
    var a = document.createElement('a');
    a.href = adRequestUrl;
    return a.href.replace(a.pathname, scriptProxyPath);
  }
 
  /**
    * Retrieves the textual content from a DOM node object.
    *
    * @param node  {object}  The node to consider
    *
    * @returns  {string}  The text contained by the node, or an empty string if no text could be retrieved.
  */
  function getNodeText(node)
  {
    if ('textContent' in node)
    {
      return node.textContent;
    }
    else if (('innerText' in node) && (node.innerText.length > 0))
    {
      return node.innerText;
    }
    else if ('text' in node)
    {
      return node.text;
    }
 
    return '';
  }

  /**
    * Sets the text of a node, if possible.
    *
    * @param node  {object}  The node to consider
    * @param text  {string}  The text to assign to the node
    *
    * @returns  {bool}  True if the text was set; otherwise, false
  */
  function setNodeText(node, text)
  {
    if ('textContent' in node)
    {
      node.textContent = text;
    }
    else if ('innerText' in node)
    {
      node.innerText = text;
    }
    else if ('text' in node)
    {
      node.text = text;
    }
    else
    {
      return false;
    }

    return true;
  }

  /**
    * Performs the tasks needed to clean a script fragment of inappropriate whitespace
    * and other malformations.
    *
    * @param text  {string}  The text to clean
    *
    * @returns  {string}  The cleaned script text
  */
  function cleanScript(text)
  {
    text = text.trim();

    if ((text.substring(0, 4) === '<!--') && (text.substring(text.length - 5) === '//-->'))
    {
      text = text.substring(4, (text.length - 5));
    }

    return text;
  }

  /**
    * Performs the tasks needed to encode a url such that it matches the
    * approach used by the proxy, allowing it to be deocded server-side.
    *
    * @param url  {string}  The url to encode
  */
  function proxyUrlencode(url) 
  {
    url = (url + '').toString();

    return encodeURIComponent(url).replace(/!/g, '%21')
                                  .replace(/'/g, '%27')
                                  .replace(/\(/g, '%28')
                                  .replace(/\)/g, '%29')
                                  .replace(/\*/g, '%2A')
                                  .replace(/%20/g, '+');
  }
 
  /**
    * Creates the XMLHttpRequest object used for issuing a url request,
    * irrespective of browser.
    *
    * @param url  {string}  [OPTIONAL] If present, the url is considered with respect to whether a cross-domain request will be issued.  If not specified, it is assumed the request is same-domain
    *
    * @returns  {object}  The XMLHttpRequestObject to use for the request, if one could be created.  Otherwise, null
  */
  function createXMLHttpRequest(url)
  {
    if (isDefined(window.XMLHttpRequest))
    {
      return new window.XMLHttpRequest();
    }
    
    try { return new ActiveXObject("Msxml2.XMLHTTP.6.0"); } catch (ex) {}
    try { return new ActiveXObject("Msxml2.XMLHTTP.3.0"); } catch (ex) {}
    try { return new ActiveXObject("Microsoft.XMLHTTP");  } catch (ex) {}
    return null;
  }
  
  /**
    * Performs the actions needed to verify that a function argument is defined as
    * a legal function before invoking it.
    *
    * @param targetFunction  {function}  The function to consider.  If the instance is a valid function, it will be invoked; otherwise, no action will be taken.
    *
    * @returns {object}  The result of the function, if available; otherwise, undefined.
  */
  function safeInvoke(targetFunction)
  {
    if ((!isDefined(targetFunction)) || (typeof(targetFunction) !== 'function'))
    {
      return undefined;
    }
  
    var args = [];
    
    if (arguments.length > 1)
    {
      for (var index = 1; index < arguments.length; ++index) 
      { 
        args.push(arguments[index]); 
      }
    }
  
    try
    {
      return targetFunction.apply(this, args);
    }
 
    catch (ex)
    {
      return undefined;
    }
  }
  
  /**
    * Performs the tasks needed to load an external script asynchronously
    * and invoke the given callback when the load is complete.  In order to maximize
    * compatibility with older browsers, the script is loaded via injection of a DOM
    * element instead of issuing an XMLHTTP request.
    *
    * @param url           {string}    The url of the external script to load
    * @param callback      {function}  [OPTIONAL] The callback function to invoke when the script load is complete.  If not supplied, no callback is invoked
    * @param container     {object}    [OPTIONAL] The DOM element used to contain the script element.  If not supplied, the <head> tag is assumed
    * @param tagAttributes {object}    [OPTIONAL] An associative array with the set of attributes to apply to the script tag.
  */  
  function loadScript(url, callback, container, tagAttributes)
  {
    container = (container || document.getElementsByTagName('head')[0]);
    var script = document.createElement('script');
 
    // If the script element has a readystate property, 
    // use that event to determine completion.  This is a work-around
    // for older versions of IE which do not fire the load event
    // reliably.
 
    if (script.readyState)
    {
      script.onreadystatechange = function()
      {
        if ((script.readyState === 'loaded') || (script.readyState === 'complete'))
        {
          script.onreadystatechange = null;
          safeInvoke(callback);
        }
      };
    }
    else
    {
      script.onload = function()
      {
        script.onload = null;
        safeInvoke(callback);
      }
    }

    if (isDefined(tagAttributes))
    {
      for (var attr in tagAttributes)
      {
        script.setAttribute(attr, tagAttributes[attr]);
      }
    }
     
    script.setAttribute('type', 'text/javascript');
    script.src = url;
    container.appendChild(script);
  }
 
  /**
    * Attempts to load and evaluate an external script in a synchronous manner.
    *
    * @param url             {string}  The url to the external script resource
    * @param scriptProxyUrl  {string}  [OPTIONAL]  The url to the proxy to use for cross-domain scripts.  If not specified, no proxy is used
    *
    * @returns  {boolean}  True if the load was successful; otherwise, false
  */
  function tryLoadScriptSynchronous(url, scriptProxyUrl)
  {
    var requestUrl = ((isCrossDomain(url)) && (isDefined(scriptProxyUrl))) ? scriptProxyUrl + proxyUrlencode(url) : url;
    var xhr        = createXMLHttpRequest(url);
    
    if (xhr === null)
    {
      return false;
    }

    function scriptProxyCallback(target)
    {
      eval(target.content);
    }
 
    try
    {
      xhr.open('GET', requestUrl, false);
      xhr.send('');
      
      // Evaluate the response.  If the request was proxied, the JSONP result will
      // cause it to be passed to the scriptProxyCallback function, where a second 
      // evaluation will be performed on the payload itself.
 
      try { eval(xhr.responseText); }    catch (ex) {}  
    }
 
    catch (ex)
    {
      return false;
    }
 
    return true;
  }

  /**
    * Performs the tasks needed to process any child scripts of a node, ensuring that
    * they have been executed.
    *
    * @param parentNode                 {object}  The DOM node to ensure child scripts for
    * @param targetContainer            {object}  The DOM element that should contain any resulting markup from script processing
    * @param alreadyProcessedAttribute  {string}  The name of the attribute present on the script element if it has already been processed
    * @param scriptProxyUrl             {string}  The base url fragment of the proxy to use for synchronous loading of cross-domain scripts
  */
  function ensureChildScripts(parentNode, targetContainer, alreadyProcessedAttribute, scriptProxyUrl)
  {
    var childScripts = parentNode.getElementsByTagName('script');
  
    if (childScripts.length <= 0)
    {
      return;
    }
  
    // In order for the script to be processed, the script node must be created by the current
    // document.  For each script that needs to be processed, create a new element and clone the
    // relevant attributes of the original source.  As the source nodes may be used for anchoring output,
    // typically based on the id attribute do not clone the id nor remove the original script from the DOM.  An
    // an in-memory container will be used to hold the clones for processing.

    var childScriptContainer = document.createElement('span');
    var childScript          = null;
    var scriptClone          = null;
    var scriptContent        = null;
  
    for (var index = 0; index < childScripts.length; ++index)
    {
      childScript = childScripts[index];

      if (childScript.hasAttribute(alreadyProcessedAttribute))
      {
        return;
      }

      scriptClone   = document.createElement('script');      
      scriptContent = childScript.getAttribute('type');
  
      if ((isDefined(scriptContent)) && (scriptContent.length > 0))
      {
        scriptClone.setAttribute('type', scriptContent);
      }
      else
      {
        scriptClone.setAttribute('type', 'text/javascript');
      }
  
      scriptContent = childScript.getAttribute('src');
  
      if ((isDefined(scriptContent)) && (scriptContent.length > 0))
      {
        scriptClone.setAttribute('src', scriptContent);
      }
  
      scriptContent = getNodeText(childScript);
  
      if ((isDefined(scriptContent)) && (scriptContent.length > 0))
      {
        setNodeText(scriptClone, scriptContent);
      }
  
      childScriptContainer.appendChild(childScript);
    }
  
    processContent(childScriptContainer, targetContainer, undefined, scriptProxyUrl, true);
  }
 
  /**
    * Performs the tasks needed to process a set of DOM elements into 
    * the page DOM, preserving synchronous context.
    *
    * @param sourceContainer    {string}    The stream of elements to parse
    * @param targetContainer    {object}    The DOM element that should contain the source content
    * @param continuation       {function}  [OPTIONAL] A function to invoke when the parsing has completed.  If not specified, no action is taken
    * @param scriptProxyUrl     {string}    [OPTIONAL] The url to the proxy to be used for sycnhronous loading of cross-domain external scripts.  If not specified, no proxy is used
    * @param trySyncScriptLoad  {boolean}   [OPTIONAL] True if an attempt should be made to load external script files syncrhonously.  If not specified, asynchronous loading is assumed    
  */
  function processContent(sourceContainer, targetContainer, continuation, scriptProxyUrl, trySyncScriptLoad)
  {
    // Parse the content.
  
    var scriptProcessedAttribute = 'data-script-processed';
    var node                     = null;
    var src                      = null;
 
    while (sourceContainer.firstChild)
    {
      node = sourceContainer.firstChild;
      src  = null;
 
      // If the node is not an element or text, then remove it and skip to processing
      // the next node.
 
      if ((node.nodeType !== ELEMENT_NODE) && (node.nodeType !== TEXT_NODE))
      {
        sourceContainer.removeChild(node);
        continue;
      }
 
      // If the node is not a script, then move it to the container and skip to
      // processing the next node.
 
      if (node.nodeName !== 'SCRIPT')
      {
        targetContainer.appendChild(node);

        if (node.nodeType === ELEMENT_NODE)
        {
          ensureChildScripts(node, targetContainer, scriptProcessedAttribute, scriptProxyUrl)
        }

        continue;
      }
 
      // If the script has no SRC attribute, then consider it an inline script snippet. 
      // Evaluate the contents and remove it from the source.
 
      src = node.getAttribute('src');
 
      if ((!isDefined(src)) || (src.length <= 0))
      {        
        targetContainer.appendChild(node);

        // If the node has an id attribute, then it is likely used as a point of reference
        // for its script.  Inject it into the DOM.

        var id = node.getAttribute('id');

        if ((isDefined(id)) && (id.length > 0))
        {
          node.innerText = '// Contents removed during parse/evaluation';
          node.setAttribute(scriptProcessedAttribute, 'true');
          targetContainer.appendChild(node);
        }

        // Capture the number of pending loads before evaluating the script.  After evaluation,
        // it can be determined if the evaluated block resulted in a pending item to be
        // waited on.

        var pendingLoadsBeforeEval = externalScriptMonitor.pendingLoads;

        try { window.eval(cleanScript(getNodeText(node))); }    catch (ex) {}    
        
        // If the evaluation resulted in an external script still pending load, then package the 
        // remainder of the processing as a continuation and take no further action.
        
        if (externalScriptMonitor.pendingLoads > pendingLoadsBeforeEval)
        {
          externalScriptMonitor.continuations.push(function() { processContent(sourceContainer, targetContainer, continuation, scriptProxyUrl, trySyncScriptLoad); }); 
          return;
        }
      }
      else
      {
        // The script is an external reference.  To mimic the behavior of a browser when loading the
        // page, the script must be loaded in its entirety before processing can continue.  The approach
        // will differ depending on whether a synchronous attempt was requested or not.  By default, an asynchronous
        // approach with continuation will be used to minimize impact on the host page responsiveness.
 
        var scriptLoaded = false;    
 
        // If the script should be loaded synchronously, attempt to do so.  If
        // successful, then no continuation is needed since execution will resume.  If
        // the script is loaded synchronously, then do not remove the original script node,
        // as another will not be injected.
 
        if (trySyncScriptLoad)
        {
          scriptLoaded = tryLoadScriptSynchronous(src, scriptProxyUrl, scriptAttributes);

          if (scriptLoaded)
          {
            node.setAttribute(scriptProcessedAttribute, 'true');
            targetContainer.appendChild(node);
          }
        }
 
        // Either no synchronous attempt was made, or the attempt failed.  Initiate an asynchronous
        // request for the script.  

        if (!scriptLoaded)
        {
          // If the node has attributes, such as an id or data-*, that could be used as a point of 
          // reference for script behaviors, then preserve them when loading the new script.

          var id = node.getAttribute('id');
          var scriptAttributes = null;
          var attributes = Array.prototype.slice.call(node.attributes);

          for (var index = 0; index < attributes.length; ++index)
          {
            attr = attributes[index];
            
            if ((isDefined(attr)) && (isDefined(attr.nodeName)))
            {
              attr = attr.nodeName;
            }
            else
            {
              attr = null;
            }

            if ((isDefined(attr)) && (attr.indexOf('type') === -1) && (attr.indexOf('src') === -1) && (attr.indexOf('id') === -1))
            {
              scriptAttributes = scriptAttributes || {};
              scriptAttributes[attr] = node.getAttribute(attr);
            }
          }

          if ((isDefined(id)) && (id.length > 0))
          {
            scriptAttributes = scriptAttributes || {};
            scriptAttributes['id'] = id;
          }

          // Remove the original script tag from the DOM, as loading the script will cause a new element to be emitted into
          // the same container, preserving any important attributes (including the id) for reference.

          sourceContainer.removeChild(node);

          // Package the remainder of the processing as a continuation to pass 
          // when the script load is complete. To accomodate external scripts loaded via a document.write call, 
          // increment the pending load counter on the external script manager.  If the script load callback invoked with
          // an additional continuation pending, then execute it.  This allows processing to pause to a continuation when an external
          // load is requested in an inline script block.  
        
          var continuationsBeforeLoad = externalScriptMonitor.continuations.length;

          var callback = function() 
          { 
            externalScriptMonitor.pendingLoads--;
            processContent(sourceContainer, targetContainer, continuation); 
  
            if (externalScriptMonitor.continuations.length > continuationsBeforeLoad)
            {
              var firstContinuation = externalScriptMonitor.continuations.pop();
              firstContinuation();
            }
          };
          
          externalScriptMonitor.pendingLoads++;
          loadScript(src, callback, targetContainer, scriptAttributes);
          return;
        }
      }              
    }
 
    // Invoke the continutaion.
 
    safeInvoke(continuation);
  }
 
  /**
    * Performs the tasks needed to parse string content into
    * the page DOM, preserving synchronous context.
    *
    * @param source             {string}    The stream of elements to parse
    * @param container          {object}    The DOM element that should contain the source content
    * @param continuation       {function}  [OPTIONAL] A function to invoke when the parsing has completed.  If not specified, no action is taken
    * @param scriptProxyUrl     {string}    [OPTIONAL] The url to the proxy to be used for sycnhronous loading of cross-domain external scripts.  If not specified, no proxy is used
    * @param trySyncScriptLoad  {boolean}   [OPTIONAL] True if an attempt should be made to load external script files syncrhonously.  If not specified, asynchronous loading is assumed    
  */
  function parseContent(source, container, continuation, scriptProxyUrl, trySyncScriptLoad)
  {
    var sourceContainer = document.createElement('div');
    sourceContainer.innerHTML = source;
    sourceContainer.normalize();
 
    processContent(sourceContainer, container, continuation, scriptProxyUrl, trySyncScriptLoad);
  }

  /**
    * Performs the actions needed to process an ad returned from mAd Serve, and
    * setting the appropriate content for it in the designated container.
    *
    * @param ad         {object}  The ad returned from mAd Serve
    * @param adConfig   {object}  The configuration provided for the ad request
    * @param container  {object}  The DOM element that should contain the ad on the page
  */
  function processAd(ad, adConfig, container)
  {
    // If the ad returned an error, then determine whether it should
    // be displayed and skip to processing the next ad.

    if (ad.error) 
    {
      if ((adConfig.backfillhtml) && (adConfig.reveal)) 
      {
        container.innerHTML = ad.error;
      }

      return;  
    }

    // If the ad was an image, then form the tag markup and insert it into
    // the container.

    if (ad.img) 
    {
      var link = document.createElement('a');
      link.setAttribute('href', ad.url);
      link.setAttribute('target', adConfig.target);

      var image = document.createElement('img');
      image.setAttribute('src', ad.img);

      if ((adConfig.img_width) && (adConfig.img_width.length > 0))
      {
        image.setAttribute('width', adConfig.img_width);
      }         
      
      if ((adConfig.img_height) && (adConfig.img_height.length > 0))
      {
        image.setAttribute('height', adConfig.img_height);
      }   

      link.appendChild(image);
      container.appendChild(link);                    
    } 
    else if (ad.content)
    {
      // If the ad has non-image content, then parse it and inject it into the container.  To
      // ensure that any document.write calls are properly handled, shim the method and restore
      // the actual implementation when complete.  The container will be hidden during parsing
      // to avoid relows as much as possible.

      container.innerHTML = '';
      var currentDisplay  = container.style.display || '';          

      var parseCompleteCallback = function()
      {
        if ('__actualWrite' in document)
        {
          document.write = document.__actualWrite;
          delete document.__actualWrite;
        }

        if ('__actualWriteln' in document)
        {
          document.writeln = document.__actualWriteln;
          delete document.__actualWriteln;
        }

        if ('__actualOpen' in document)
        {
          document.open = document.__actualOpen;
          delete document.__actualOpen;
        }

        if ('__actualClose' in document)
        {
          document.close = document.__actualClose;
          delete document.__actualClose;
        }

        container.style.display = currentDisplay; 
      };

      // Create a shim for document.write methods that will parse the content.  Ignore
      // any pending script loads, as any script references will be loaded synchrouously.  It
      // is possible/likely that the document.write call was made from an external script currently
      // being loaded asynchronously.

      var proxyUrl  = buildScriptProxyUrl(adConfig.requesturl);
      var writeShim = function(string) { parseContent(string, container, undefined, proxyUrl, true); };
      var emptyShim = function() {};

      document.__actualWrite   = document.write;  
      document.__actualWriteln = document.writeln; 
      document.__actualOpen    = document.open;
      document.__actualClose   = document.close;       
      document.write           = writeShim;
      document.writeln         = writeShim;
      document.open            = emptyShim;
      document.close           = emptyShim;

      container.style.display = 'none';
      parseContent(ad.content, container, parseCompleteCallback, proxyUrl, false);
    } 
    else 
    {
      // The ad is text-based only.  Create an anchor using the ad text
      // as it's content.

      var link = document.createElement('a');
      link.setAttribute('href', ad.url);
      link.setAttribute('target', adConfig.target);
      link.appendChild(document.createTextNode(ad.text));

      container.appendChild(link);
    }
  }

  /**
    * Creates a callback function to be invoked when a requested ad is loaded.
    *
    * @param adId {string}  The identifier of the ad
    * @param container {object}  The DOM element that serves as the container for the ad
  */
  function createAdLoadedCallback(adId, container, adConfig, swipeCallback)
  {
    return function()
    {
      var ads = window[adId];
      var ad  = null;

      // Process each ad that was returned as part of the request.

      for (var index=0; index < ads.length; ++index) 
      {
        ad = ads[index];
        processAd(ad, adConfig, container);
        
        // Create the tracking pixel for the ad.  Setting the
        // src will trigger the request; the object does not need
        // to be added to the page DOM.

        var track = new Image();
        track.src = ad.track;
      }

      // If the backfill content is present after processing, 
      // then ensure that it is displayed.

      var dcont = document.getElementById('m_dcontent' + adId);
      
      if (dcont)
      {
        dcont.style.display = 'block';
      }

      // If configuration defines a prepend item for clickables, then 
      // process all links in the the ad container and prepend.

      if (adConfig.prependclickcontent)
      {
        var links = container.getElementsByTagName('a');
        var link  = null;

        for (var index=0; index < links.length; ++index)
        {
           link = links[index];
           link.href = (adConfig.prependclickcontent + link.href);
        }
      }

      // If a request-level tracking pixel was registered, then
      // create it.  Setting the src will trigger the request; the
      // object does not need to be added to the page DOM.

      if (adConfig.trackingpixelurl)
      {
        var hasQuery = (adConfig.trackingpixelurl.indexOf('?') !== -1);
        var image    = new Image();

        img.src = (adConfig.trackingpixelurl + (hasQuery ? '&' : '?') + Math.random());
      }

      // Invoke the swipe callback.

      safeInvoke(swipeCallback);
    };
  }

  /**
    * Performs the tasks needed to request an ad from mAd Serve.
    *
    * @param adConfig  {object}  The set of configuration properties to apply to the ad request 
  */
  function requestAd(adConfig)
  {
    var adId                = 'global_ad_id' + (++adCount);
    var swipeId             = function(id) { return id; }(window.currentSwipeAdId);
    var invokeSwipeCallback = function() { safeInvoke(window.swipeAdCompleteCallback, swipeId); };

    if (!adConfig.s) 
    {
      return;
    }

    // Due to the script being asynchronous, signal to the Swipe
    // platform that it should await a callback rather than performing 
    // it's actions.
    
    if (window.SwipeAdState) 
    {
      window.swipeState = window.SwipeAdState.AwaitCallback;
    }

    // Emit an element to use an a container for injecting the ad when the asynchronous
    // call to retrieve it completes.  Use document.write() to ensure that the ad renders
    // in the proper location in the page.

    document.write("<div id='" + adId + "'></div>");
    var container = document.getElementById(adId);

    // If backfill markup was provided, populate it in the event that the
    // ad is not returned.

    if (adConfig.backfillhtml)
    {
      container.innerHTML = '<div id= "m_dcontent' + adId + '" style="display:none">' + adConfig.backfillhtml + '</div>';
    }

    // Configure the ad parameters.  

    if (!adConfig.target) 
    {
      adConfig.target = '';
    }  

    var cgi = 
    [
      'p=' + escape(document.location),
       'random=' + Math.random(),
       'rt=javascript',
       'v=js_10',
       'jsvar=' + adId,
       'u=' + navigator.userAgent
    ];

    for (var setting in adConfig) 
    {
      cgi.push(setting + '=' + escape(adConfig[setting]));
    }

    // Create the ad request, sending it once the document is fully ready.

    var requestUrl = (adConfig.service !== undefined) ? adConfig.service : adConfig.requesturl;
    requestUrl += "?" + cgi.join('&');
    
    var adCallback = createAdLoadedCallback(adId, container, adConfig, invokeSwipeCallback); 

    var requestCallback = function()
    {
      container = document.getElementById(adId);
      adCallback();
    };
    
    DOMReady.add(function()
    {
      loadScript(requestUrl, requestCallback, container);
    });
  }

  //  ==================================================
  // |  Initializations and Public Member Declarations  |
  //  ==================================================

  if ((!isDefined(window.RequestAd_)) || (typeof(window.RequestAd_) !== 'function'))
  {
    window.RequestAd_ = requestAd;
  }

  if (!('trim' in String.prototype))
  {
    String.prototype.trim = function() { return (this === null) ? null : this.replace(/^\s\s*/, '').replace(/\s\s*$/, ''); }
  }

})(window, document);