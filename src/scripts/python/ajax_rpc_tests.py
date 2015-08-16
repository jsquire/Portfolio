import json
from django import template
from django.test import TestCase
from django.test.client import RequestFactory
from django.contrib.auth.models import User, Permission
from django.contrib.contenttypes.models import ContentType
from application.userprofiles.models import UserProfile
from application.core.ajax_rpc import *


class SimpleAjaxResultTests(TestCase):
    def test_result_constants_are_defined(self):
        self.assertTrue(hasattr(SimpleAjaxResult, 'SUCCESS'))
        self.assertTrue(hasattr(SimpleAjaxResult, 'FAILURE'))

    def test_init_applies_defaults(self):
        result = SimpleAjaxResult()
        self.assertEqual(SimpleAjaxResult.FAILURE, result.result)
        self.assertEqual(None, result.markup)
        self.assertEqual(None, result.messages)
        self.assertEqual(None, result.data)

    def test_init_applies_kwargs(self):
        result_code = SimpleAjaxResult.SUCCESS
        markup = 'markup value'
        messages = ['messages', 'list']
        data = {'key' : 'value' }
        result = SimpleAjaxResult(result=result_code, markup=markup, messages=messages, data=data)


        self.assertEqual(result_code, result.result)
        self.assertEqual(markup, result.markup)
        self.assertEqual(messages, result.messages)
        self.assertEqual(data, result.data)

    def test_serialization(self):
        result_code = SimpleAjaxResult.SUCCESS
        markup = 'markup value'
        messages = ['messages', 'list']
        data = {'key' : 'value' }
        result = SimpleAjaxResult(result=result_code, markup=markup, messages=messages, data=data)
        json = result.serialize()

        self.assertTrue(json)
        self.assertTrue(len(json) > 0)
        self.assertTrue(str(result_code) in json)
        self.assertTrue(markup in json)

        for item in messages:
            self.assertTrue(item in json)

        for key in data:
            self.assertTrue(key in json)
            self.assertTrue(data[key] in json)


@ajax_permission_required('userprofiles.change_userprofile')
def permission_test_stub(request, *args, **kwargs):
    result = SimpleAjaxResult(result=SimpleAjaxResult.SUCCESS, markup='pass')
    return HttpResponse(content=result.serialize(), content_type='test/result')

@ajax_login_required
def login_test_stub(request, *args, **kwargs):
    result = SimpleAjaxResult(result=SimpleAjaxResult.SUCCESS, markup='pass')
    return HttpResponse(content=result.serialize(), content_type='test/result')

class DecoratorTests(TestCase):
    def setUp(self):
        self.factory = RequestFactory()

        # Create a user without the proper permission
        self.normal = User.objects.create(username='normal')
        self.normal.set_password('normaluser')
        self.normal.save()
       
        # Create a user with the needed permission
        user_profile_content_type = ContentType.objects.get_for_model(UserProfile)
        permission_change_user = Permission.objects.get(content_type=user_profile_content_type, codename='change_userprofile')
        self.normal2 = User.objects.create(username='normal2')
        self.normal2.set_password('normaluser2')
        self.normal2.user_permissions.add(permission_change_user)
        self.normal2.save()

    def test_ajax_login_required(self):
        request = self.factory.get('/user/somepage')
        request.user = None
        request.session = None

        # Unauthenticated - triggers the decorator
        result = login_test_stub(request)
        self.assertEqual('application/json', result['Content-Type'])
        self.assertEqual('INVALID-VALUE', result['X-Frame-Options'])

        ajax = json.loads(result.content)
        self.assertEqual(1, len(ajax))

        ajax = ajax[0]
        self.assertEqual(SimpleAjaxResult.FAILURE, ajax['result'])
        self.assertEqual(None, ajax['markup'])
        self.assertTrue(len(ajax['messages']) > 0)

        # Authenticated - does not trigger the decorator
        request.user = self.normal
        request.session = {}
        result = login_test_stub(request)
        self.assertEqual('test/result', result['Content-Type'])

        ajax = json.loads(result.content)
        self.assertEqual(1, len(ajax))

        ajax = ajax[0]
        self.assertEqual(SimpleAjaxResult.SUCCESS, ajax['result'])
        self.assertEqual('pass', ajax['markup'])

    def test_ajax_permission_required(self):
        request = self.factory.get('/user/somepage')
        request.user = None
        request.session = None

        # Unauthenticated - triggers the decorator
        result = permission_test_stub(request)
        self.assertEqual('application/json', result['Content-Type'])
        self.assertEqual('INVALID-VALUE', result['X-Frame-Options'])

        ajax = json.loads(result.content)
        self.assertEqual(1, len(ajax))

        ajax = ajax[0]
        self.assertEqual(SimpleAjaxResult.FAILURE, ajax['result'])
        self.assertEqual(None, ajax['markup'])
        self.assertTrue(len(ajax['messages']) > 0)

        # Authenticated, No Permission - triggers the decorator
        request.user = self.normal
        request.session = {}
        result = permission_test_stub(request)
        self.assertEqual('application/json', result['Content-Type'])
        self.assertEqual('INVALID-VALUE', result['X-Frame-Options'])

        ajax = json.loads(result.content)
        self.assertEqual(1, len(ajax))

        ajax = ajax[0]
        self.assertEqual(SimpleAjaxResult.FAILURE, ajax['result'])
        self.assertEqual(None, ajax['markup'])
        self.assertTrue(len(ajax['messages']) > 0)

        # Authenticated, With Permission - does not trigger the decorator
        request.user = self.normal2
        request.session = {}
        result = permission_test_stub(request)
        self.assertEqual('test/result', result['Content-Type'])

        ajax = json.loads(result.content)
        self.assertEqual(1, len(ajax))

        ajax = ajax[0]
        self.assertEqual(SimpleAjaxResult.SUCCESS, ajax['result'])
        self.assertEqual('pass', ajax['markup'])


   
