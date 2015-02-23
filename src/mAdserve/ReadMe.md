# mAdserve #

### Summary ###

[mAdserve](http://madserve.org "mAdserve") is an open source platform for serving mobile ads.  It's goal is to allow an ad publisher to track, sell, and deliver ads to targeted locations on a host page.  The platform offers the ability to integrate with many ad networks to match advertiser stock with the publisher's locations.

Many ads, both static and rich content, attempt to be compatible with as many browsers as possible, including those that mainstream web applications have dismissed as legacy a long while back.  As a result, those ads that attempt to offer rich content using client-side script often use the approach of issuing a `document.write` call to render all or part of their markup, which allows them to deterministically inject HTML into the host page in the exact location that the ad script was referenced.

When delivering ads, mAdServe attempts to minimize impact to the host pages by requesting ads asynchronously using its own client-side script, rather than allowing the ads to render directly.  For static ads comprised of nothing but HTML and image content, this approach works well.  However, for rich media ads this causes issues as the behavior of `document.write` changes dramatically after the page has rendered.  Rather than injecting markup inline where the script was referenced, calling `document.write ` after page rendering is complete will overwrite the content of the page.  Since most ad content assumes that it is being rendered synchronously, this resulted in major issues in the host page when mAdserve was used to deliver rich ads with scripted behaviors.

### Assets ###

Developed in early 2013, the assets herein attempt to overcome the issues that plagued the default mAdserve approach of serving ads asynchronously by building up a client-side framework that was able to accommodate ads intended to be rendered synchronously by shimming the default browser behaviors that they rely upon.  Once the rendering is complete, the default browser behavior is restored to ensure that the host page behaves as intended.

Included in this implementation are two files:

* **ad.js** - This is the rewritten mAdserve client-side behavior script extended to allow for asynchronous rendering of both static and rich mobile ads.

* **madserve-script-proxy.php** - This is a server-side addition to the mAdserve platform that serves as a lightweight and simple proxy to allow ad source script files to be requested from domains not belonging to the host page.

### Known Issues ###

While the core functionality works relatively well for most cases, there are a few issues that had not been resolved when the initial development was completed.  Those issues are:

* **Ads Injecting Scripts Line-by-Line ** Because of the approach used to mimic the browser's default behavior when calling `document.write` during the initial page rendering, any script content injected using a `document.write` call was evaluated immediately to ensure that it was in scope for the remaining content of the ad.  This worked well in most cases, except for when ads injected scripts in the fashion of emitting each line with a unique `document.write` call.  This results in the client-side framework attempting to evaluate incomplete, and often illegal, script fragments.  Prototyping was underway to detect and rewrite this pattern when the decision was made to move away from mAdserve.

* **Ads Contained in iFrames** - Part of the rendering strategy for the client-side framework was to render the ad in a known container element to keep it from appearing during the rendering process, as this may need to make several network requests that would impact the ad's intended user experience.  Once the ad was fully rendered, it was moved out of the off-screen container and into its proper location on the page.  Unfortunately, the content of an iframe is reset whenever the element is moved within the DOM.  Because ads that render their content in an iframe typically also alter the contents via script to control the ad's behaviors, moving the ad causes the iframe content to be reset.  This issue was under investigation when the decision was made to move away from mAdserve.  

*  **Support for Internet Explorer 8 and Below** - Because the client-side framework attempts to mimic behavior of the browser when the ad is rendered synchronously during the initial page rendering, any client scripts that the ad injects during its rendering need to be requested synchronously and parsed to ensure they are in scope for the ad code that attempts to reference them.  Internet Explorer browsers earlier than version 9 do not allow an XmlHttp request to be made synchronously.  Because the specific use of mAdserve in this case was to target mobile browsers only, this was deemed an acceptable defect that would not be fixed. 



