import json
from functools import wraps
from django.http import HttpResponse


class SimpleAjaxResult(object): 
    """
      A generic structure for returning API results intended for 
      server-side rendered AJAX calls.
    """    
    SUCCESS = 0
    FAILURE = 1

    def __init__(self, **kwargs):        
        self.result = kwargs.get('result', SimpleAjaxResult.FAILURE)
        self.markup = kwargs.get('markup', None)
        self.messages = kwargs.get('messages', None)
        self.data = kwargs.get('data', None)

        super(SimpleAjaxResult, self).__init__()

    def serialize(self):
        return json.dumps([self.__dict__])


def ajax_permission_required(permission):
    """
      Provides a decorator that mirrors the Django permission_required decorator, returning
      an ajax result instead of redirecting on failure.
    """
    def decorator(target_view):
        def wrapped_view(request, *args, **kwargs):
            if request.user and request.user.is_authenticated() and request.user.has_perm(permission):
                return target_view(request, *args, **kwargs)

            result = SimpleAjaxResult(result=SimpleAjaxResult.FAILURE, messages=['The user does not have proper permissions for this action.'])
            response = HttpResponse(content=result.serialize(), content_type='application/json')
            response['X-Frame-Options'] = 'INVALID-VALUE'            
            return response
        return wraps(target_view)(wrapped_view)
    return decorator


def ajax_login_required(target_view):
    """
      Provides a decorator that mirrors the Django login_required decorator, returning
      an ajax result instead of redirecting on failure.
    """
    def wrapped_view(request, *args, **kwargs):
        if request.user and request.user.is_authenticated():
            return target_view(request, *args, **kwargs)

        result = SimpleAjaxResult(result=SimpleAjaxResult.FAILURE, messages=['Login is required for this action.'])
        response = HttpResponse(content=result.serialize(), content_type='application/json')
        response['X-Frame-Options'] = 'INVALID-VALUE'
        return response
    return wraps(target_view)(wrapped_view)
