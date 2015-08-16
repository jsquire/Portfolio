
import sys
import os
import urllib2
from xml.dom import minidom

class SmartHttpHandler(urllib2.HTTPHandler):
    def do_open(self, http_class, req):
        result = urllib2.HTTPHandler.do_open(self, http_class, req)
        if not hasattr(result, 'status'): result.status = 200
        return result

class SmartRedirectHandler(urllib2.HTTPRedirectHandler):     
    def http_error_301(self, req, fp, code, msg, headers):         
        result = urllib2.HTTPRedirectHandler.http_error_301(self, req, fp, code, msg, headers)              
        result.status = code                                 
        return result                                       

    def http_error_302(self, req, fp, code, msg, headers):           
        result = urllib2.HTTPRedirectHandler.http_error_302(self, req, fp, code, msg, headers)              
        result.status = code                                
        return result         


def build_opener():
    return urllib2.build_opener(SmartRedirectHandler(), SmartHttpHandler())


def verify(url):
    try:    
        request = urllib2.Request(url)
        opener = build_opener()
        result = opener.open(request)

    except:
        pass

    finally:
        result.close()
        opener.close()

    return (result.status == 200), str(result.status), result.geturl(), url


def processMap(path, out_file, indent):    
    dom = minidom.parse(path)
    locElements = dom.getElementsByTagName('loc')

    for element in locElements:        
        result, status, actual_url, requested_url = verify(''.join(text.nodeValue for text in element.childNodes if text.nodeType == text.TEXT_NODE))
      
        if (result):
            sys.stdout.write('.')            
        else:
            sys.stdout.write('X')
            out_file.write('{0}[{1}] {2} ==> {3}\n'.format(indent, status, requested_url, actual_url))


def main(args):        
    indent =  '    '

    for arg in args:
        out_file_name = '{0}_results.txt'.format(arg)

        print('\r\n================================================')
        print('Processing [{0}]'.format(arg))
        
        with open(out_file_name, 'w') as out_file:
            processMap(os.path.join(sys.path[0], arg), out_file, indent)

        print('\r\n\r\nResults written to: {0}'.format(out_file_name))
        print('================================================\r\n')



if __name__ == '__main__':
    main(sys.argv[1:])



