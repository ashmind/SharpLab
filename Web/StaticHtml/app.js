/* globals CodeMirror:false, config:false */

$(function() {
  'use strict';
  var storageKey = '__tryroslyn__code__';
  var editor = CodeMirror.fromTextArea($('#code textarea')[0], {
    mode:        'text/x-csharp',
    lineNumbers: true,
    indentUnit:  4
  });
    
  var decompiled = CodeMirror.fromTextArea($('#decompiled textarea')[0], {
    mode:     'text/x-csharp',
    readOnly: true
  });
  
  load(editor);
  editor.on('change', $.debounce(600, function() {
    save(editor);
    update();
  }));
  update();
  
  function save() {
    localStorage.setItem(storageKey, editor.getValue());
  }
  
  function load() {
    var value = localStorage.getItem(storageKey);
    if (value === undefined || value === null)
        value = getDefaultValue();

    editor.setValue(value);
  }
  
  function getDefaultValue() {
    var value = $(editor.getWrapperElement())
                   .siblings('script[data-default]')
                   .text()
                   .trim();
    var lines = value.split(/[\r\n]+/g);
    var indent = lines[lines.length - 1].match(/^\s*/)[0];
    return value.replace(new RegExp(indent, 'g'), '');
  }
    
  function update() {
    var code = editor.getValue();
    $.ajax("api/compilation", {
      method: 'POST',
      data: code,
      contentType: 'text/x-csharp'
    }).done(function(result) {
      clearErrors();
      decompiled.setValue(result);
    }).fail(function(xhr) {
      reportErrors(xhr.responseText);
    });
  }
  
  function reportErrors(errors) {
    $('header').addClass('error');
    decompiled.setValue(errors);
  }
  
  function clearErrors() {
    $('header').removeClass('error');
  }
});