<?php
  /*
    This script serves as a simplistic proxy to allow ad scripts to be loaded from different domains
    during asynchronous ad rendering.
  */

  header('Content-Type: application/json; charset=utf-8');
  header('Access-Control-Allow-Origin: *');
  
  // Define configuration to control debug logging.

  define('ADSERVE_DEBUG_LOG',      false);  
  define('ADSERVE_DEBUG_LOG_FILE', '/tmp/madserve-debug.log');  
  
  // Retrieve the script url and decode it.  Because
  // many ad providers use urls within their querystring
  // values, parse the scriptUrl and ensure that each
  // of the querystring values is properly reencoded.

  $scriptUrl = urldecode($_GET['url']);
  $query     = parse_url($scriptUrl);
  $query     = array_key_exists("query", $query) ? $query["query"] : '';

  if (ADSERVE_DEBUG_LOG) 
  {
    $debugLog = fopen(ADSERVE_DEBUG_LOG_FILE, 'a');
    fwrite($debugLog, "Begin logged script-proxy.php call.\n");
    fwrite($debugLog, "Query: " . $query . "\n");
    fwrite($debugLog, "URL: " . $scriptUrl . "\n");
  }

  // Capture the querystring arguments and break them into a set so that they
  // can be properly processed when building the ad script url.

  if (strlen($query) > 0)
  {
    $query = html_entity_decode($query);
    $query = explode('&', $query);
    $arr   = array();

    foreach($query as $val)
    {
      $qKey          = explode('=', $val);
      $arr[$qKey[0]] = $qKey[1];
    }

    unset($val, $qKey, $query);

    // Capture the desired ad script source without any querystring arguments in place.
    
    $scriptUrl = substr($scriptUrl, 0, (strpos($scriptUrl, '?') + 1));
    $amper = false;

    // Rebuild the querystring needed for the ad source url and ensure that it is properly 
    // encoded.

    foreach($arr as $key => $val)
    {
      if ($amper)
      {
        $scriptUrl .= '&';
      }

      $scriptUrl .= $key . '=' . urlencode($val);
      $amper = true;
    }
  }

  if (ADSERVE_DEBUG_LOG) 
  {
    fwrite($debugLog, "Setting up for cURL call:\n");
    fwrite($debugLog, "URL: " . $scriptUrl . "\n");
    fwrite($debugLog, "User Agent:" . $_SERVER['HTTP_USER_AGENT'] . "\n");
  }

  // Request the ad script from the source, passing along the proxy request header
  // values to maintain as much of the integrity of the original request as possible.

  $curl = curl_init();
  curl_setopt($curl, CURLOPT_URL, $scriptUrl);
  curl_setopt($curl, CURLOPT_RETURNTRANSFER, 1);
  curl_setopt($curl, CURLOPT_USERAGENT, $_SERVER['HTTP_USER_AGENT']);
  curl_setopt($curl, CURLOPT_CONNECTTIMEOUT, 30);
  curl_setopt($curl, CURLOPT_HEADER, false);
  curl_setopt($curl, CURLOPT_FOLLOWLOCATION, true);
  curl_setopt($curl, CURLOPT_MAXREDIRS, 3);

  $response = curl_exec($curl);
  curl_close($curl);

  // If the requested ad source script is not available, return an HTTP 404 and cease 
  // processing.

  if ((is_bool($response)) && ($response === false))
  {
    header("Status: 404 Not Found");
    exit(0);
  }

  if (ADSERVE_DEBUG_LOG) 
  {
    fwrite($debugLog, "Returning:\n" . $response . "\n");
    fwrite($debugLog, "End logged script-proxy.php call.\n");
    fclose($debugLog);
  }

  // Send the ad script response as the payload using a JSONP-stype callback.

  echo "scriptProxyCallback({ content: " . json_encode($response) . "});"
?>