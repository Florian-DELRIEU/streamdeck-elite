// Property inspector of the generic actions ("Donnee" = com.mhwlng.elite.value; Alarm later) - see docs/L4-donnee.md.
// Needs sdtools.common.js, catalog.js and commands.js (generated at build time by Elite.CatalogGen).
// The real settings are the elements of class sdProperty; the view / category / search / field controls only help
// to fill the key of the view being edited (source, source2, source3, source4).
// Keep this file ASCII-only (non-ASCII characters are written as \u escapes).

var GENERIC_ACTION_KINDS = {
    'com.mhwlng.elite.value': 'value'
};

var GENERIC_TYPE_LABELS = {
    'bool': 'oui/non',
    'number': 'nombre',
    'text': 'texte',
    'date': 'date/heure',
    'enum': 'liste de valeurs'
};

// tests offered in the image rules, by type of the main value (section 6 of the specification)
var GENERIC_RULE_OPERATORS = {
    'bool': ['isTrue', 'isFalse', 'equals', 'notEquals'],
    'number': ['isTrue', 'isFalse', 'equals', 'notEquals', 'lt', 'lte', 'gt', 'gte'],
    'text': ['isTrue', 'isFalse', 'equals', 'notEquals'],
    'enum': ['isTrue', 'isFalse', 'equals', 'notEquals'],
    'date': ['isTrue', 'isFalse']
};

var GENERIC_MAX_RESULTS = 100;
var GENERIC_VIEWS = 4;
var GENERIC_RULES = 4;

var genericIndex = null;     // built once from ELITE_CATALOG
var genericActiveView = 1;   // view whose key is being edited

// ---------- catalog index (pure functions) ----------

function genericBuildIndex(catalog) {
    var index = { categories: [], categoryById: {}, groupsByCategory: {}, byKey: {}, entries: [], enums: catalog.enums || {} };

    catalog.categories.forEach(function (category, order) {
        category.order = order;
        index.categories.push(category);
        index.categoryById[category.id] = category;
        index.groupsByCategory[category.id] = [];
    });

    catalog.groups.forEach(function (group) {
        if (!index.groupsByCategory[group.category])
            return;
        index.groupsByCategory[group.category].push(group);
        group.entries = group.fields.map(function (field) {
            var entry = {
                key: group.prefix + '.' + field[0],
                path: field[0],
                type: field[1],
                enumName: field[2] || null,
                group: group,
                category: group.category
            };
            index.byKey[entry.key.toLowerCase()] = entry;
            index.entries.push(entry);
            return entry;
        });
    });

    return index;
}

function genericFindEntry(index, key) {
    if (!index || !key)
        return null;
    return index.byKey[String(key).trim().toLowerCase()] || null;
}

function genericCompare(a, b) {
    a = String(a).toLowerCase();
    b = String(b).toLowerCase();
    return a < b ? -1 : a > b ? 1 : 0;
}

// every word of the text must appear in the key or in the group label (case-insensitive)
function genericSearch(index, text, max) {
    var words = String(text || '').toLowerCase().split(/\s+/).filter(function (w) { return w.length > 0; });
    if (words.length === 0)
        return { entries: [], total: 0 };

    var matches = index.entries.filter(function (entry) {
        var haystack = (entry.key + ' ' + entry.group.label).toLowerCase();
        return words.every(function (w) { return haystack.indexOf(w) >= 0; });
    });

    // category, then group, then field: entries of a group stay together in the list
    matches.sort(function (a, b) {
        var ca = index.categoryById[a.category].order, cb = index.categoryById[b.category].order;
        if (ca !== cb)
            return ca - cb;
        return genericCompare(a.group.label, b.group.label) || genericCompare(a.group.id, b.group.id) || genericCompare(a.path, b.path);
    });

    return { entries: matches.slice(0, max), total: matches.length };
}

function genericGroupLabel(group) {
    return group.label + (group.odyssey ? ' (Odyssey)' : '');
}

function genericDescribe(index, key) {
    if (!key)
        return { text: 'Choisissez une donn\u00e9e, ou tapez une cl\u00e9.', warning: false };

    var entry = genericFindEntry(index, key);
    if (!entry)
        return { text: '\u26a0 Cl\u00e9 hors catalogue (saisie manuelle) : elle fonctionnera si le jeu l\'\u00e9crit.', warning: true };

    var text = 'Type : ' + (GENERIC_TYPE_LABELS[entry.type] || entry.type) + ' \u00b7 ' + genericGroupLabel(entry.group);
    if (entry.type === 'enum' && index.enums[entry.enumName]) {
        var values = index.enums[entry.enumName];
        text += ' \u00b7 valeurs : ' + values.slice(0, 6).join(', ') + (values.length > 6 ? ', \u2026' : '');
    }
    return { text: text, warning: false };
}

// ---------- user interface ----------

function genericElement(id) {
    return document.getElementById(id);
}

function genericKeyInput(view) {
    return genericElement('source' + (view === 1 ? '' : view));
}

function genericInit() {
    if (genericIndex || typeof ELITE_CATALOG === 'undefined' || !genericElement('genericCategory'))
        return;

    genericIndex = genericBuildIndex(ELITE_CATALOG);

    var select = genericElement('genericCategory');
    select.innerHTML = '';
    select.appendChild(genericOption('', '\u2014 choisir une cat\u00e9gorie \u2014'));
    genericIndex.categories.forEach(function (category) {
        var count = genericIndex.groupsByCategory[category.id].reduce(function (n, g) { return n + g.entries.length; }, 0);
        select.appendChild(genericOption(category.id, category.emoji + ' ' + category.label + ' (' + count + ')'));
    });

    genericFillCommands();
    genericFillFields(null);
    genericShowSections();
    genericShowView();
    genericSyncFromSource();
}

function genericOption(value, text) {
    var option = document.createElement('option');
    option.value = value;
    option.textContent = text;
    return option;
}

function genericFieldOption(entry) {
    return genericOption(entry.key, entry.path + ' \u2014 ' + (GENERIC_TYPE_LABELS[entry.type] || entry.type));
}

// keyboard commands (commands.js), grouped by binding file
function genericFillCommands() {
    var select = genericElement('pressCommand');
    if (!select || typeof ELITE_COMMANDS === 'undefined')
        return;

    select.innerHTML = '';
    select.appendChild(genericOption('', '\u2014 aucune \u2014'));
    ELITE_COMMANDS.groups.forEach(function (group) {
        var optgroup = document.createElement('optgroup');
        optgroup.label = group.label;
        group.commands.forEach(function (command) {
            var option = genericOption(command[0], command[1]);
            option.title = command[0];
            optgroup.appendChild(option);
        });
        select.appendChild(optgroup);
    });
}

// entries: list of catalog entries (grouped in the list by event / status part), or null for an empty list
function genericFillFields(entries, placeholder) {
    var select = genericElement('genericField');
    select.innerHTML = '';
    select.appendChild(genericOption('', placeholder || '\u2014 choisir une donn\u00e9e \u2014'));
    if (!entries)
        return;

    var currentGroup = null, optgroup = null;
    entries.forEach(function (entry) {
        if (entry.group !== currentGroup) {
            currentGroup = entry.group;
            optgroup = document.createElement('optgroup');
            optgroup.label = genericGroupLabel(entry.group);
            select.appendChild(optgroup);
        }
        optgroup.appendChild(genericFieldOption(entry));
    });
}

function genericEntriesOfCategory(categoryId) {
    var entries = [];
    (genericIndex.groupsByCategory[categoryId] || []).forEach(function (group) {
        entries = entries.concat(group.entries);
    });
    return entries;
}

function genericSelectField(key) {
    var select = genericElement('genericField');
    var wanted = String(key || '').toLowerCase();
    for (var i = 0; i < select.options.length; i++) {
        if (select.options[i].value.toLowerCase() === wanted && wanted !== '') {
            select.selectedIndex = i;
            return;
        }
    }
    select.selectedIndex = 0;
}

function genericUpdateKeyInfo() {
    var info = genericDescribe(genericIndex, genericKeyInput(genericActiveView).value);
    var element = genericElement('genericKeyInfo');
    element.textContent = info.text;
    element.className = 'sdpi-item-value generic-info' + (info.warning ? ' generic-warning' : '');
}

// category and field lists follow the key of the edited view
function genericSyncFromSource() {
    if (!genericIndex)
        return;

    var entry = genericFindEntry(genericIndex, genericKeyInput(genericActiveView).value);
    genericElement('genericSearch').value = '';
    if (entry) {
        genericElement('genericCategory').value = entry.category;
        genericFillFields(genericEntriesOfCategory(entry.category));
        genericSelectField(entry.key);
    } else {
        genericElement('genericCategory').value = '';
        genericFillFields(null);
    }
    genericUpdateKeyInfo();
    genericUpdateRuleHelpers();
}

// only the fields of the edited view are shown
function genericShowView() {
    var blocks = document.querySelectorAll('[data-view]');
    Array.prototype.forEach.call(blocks, function (block) {
        block.style.display = block.getAttribute('data-view') === String(genericActiveView) ? '' : 'none';
    });
}

function genericViewChanged() {
    genericActiveView = parseInt(genericElement('genericView').value, 10) || 1;
    genericShowView();
    genericSyncFromSource();
}

function genericCategoryChanged() {
    genericElement('genericSearch').value = '';
    var categoryId = genericElement('genericCategory').value;
    genericFillFields(categoryId ? genericEntriesOfCategory(categoryId) : null);
    genericSelectField(genericKeyInput(genericActiveView).value);
}

function genericSearchChanged() {
    var text = genericElement('genericSearch').value;
    if (text.trim().length < 2) {
        genericCategoryChanged();
        return;
    }

    genericElement('genericCategory').value = '';
    var result = genericSearch(genericIndex, text, GENERIC_MAX_RESULTS);
    var placeholder = result.total === 0
        ? '\u2014 aucun r\u00e9sultat \u2014'
        : '\u2014 ' + result.total + ' r\u00e9sultat(s)' + (result.total > GENERIC_MAX_RESULTS ? ', ' + GENERIC_MAX_RESULTS + ' affich\u00e9s : pr\u00e9cisez' : '') + ' \u2014';
    genericFillFields(result.entries, placeholder);
    genericSelectField(genericKeyInput(genericActiveView).value);
}

function genericFieldChanged() {
    var key = genericElement('genericField').value;
    if (!key)
        return;
    genericKeyInput(genericActiveView).value = key;
    genericUpdateKeyInfo();
    genericUpdateRuleHelpers();
    setSettings();
}

// manual entry of a key (any key, even outside the catalog)
function genericSourceTyped() {
    genericUpdateKeyInfo();
    genericUpdateRuleHelpers();
    setSettings();
}

// image rules: tests offered according to the type of the main value (view 1), enum values proposed as operands
function genericUpdateRuleHelpers() {
    var main = genericKeyInput(1);
    if (!main || !genericIndex)
        return;

    var entry = genericFindEntry(genericIndex, main.value);
    var allowed = entry ? GENERIC_RULE_OPERATORS[entry.type] : null;
    for (var i = 1; i <= GENERIC_RULES; i++) {
        var select = genericElement('rule' + i + 'Op');
        if (!select)
            continue;
        Array.prototype.forEach.call(select.options, function (option) {
            // never disable the current choice, so that a saved rule stays visible
            option.disabled = option.value !== '' && allowed !== null && allowed.indexOf(option.value) < 0 && option.value !== select.value;
        });
    }

    var list = genericElement('genericEnumValues');
    if (!list)
        return;
    list.innerHTML = '';
    var values = [];
    if (entry && entry.type === 'enum' && genericIndex.enums[entry.enumName])
        values = genericIndex.enums[entry.enumName];
    else if (entry && entry.type === 'bool')
        values = ['oui', 'non'];
    values.forEach(function (value) {
        list.appendChild(genericOption(value, value));
    });
}

// sections marked data-actions="value alarm" are shown for these actions only
function genericShowSections() {
    var kind = (typeof actionInfo !== 'undefined' && actionInfo && actionInfo.action) ? GENERIC_ACTION_KINDS[actionInfo.action] : null;
    var sections = document.querySelectorAll('[data-actions]');
    Array.prototype.forEach.call(sections, function (section) {
        var kinds = section.getAttribute('data-actions').split(' ');
        section.style.display = (!kind || kinds.indexOf(kind) >= 0) ? '' : 'none';
    });
}

// sdtools.common.js calls loadConfiguration when the settings arrive. The lists (commands...) must exist before the
// values are loaded, and the view / category lists are resynchronised afterwards.
(function () {
    var baseLoadConfiguration = window.loadConfiguration;
    window.loadConfiguration = function (payload) {
        try {
            genericInit();
        } catch (err) {
            console.log('generic.js init: ' + err);
        }

        baseLoadConfiguration(payload);

        try {
            // keys created before "showText" existed show their value
            if (payload && !Object.prototype.hasOwnProperty.call(payload, 'showText') && genericElement('showText'))
                genericElement('showText').checked = true;
            genericShowSections();
            genericShowView();
            if (payload && Object.prototype.hasOwnProperty.call(payload, 'source'))
                genericSyncFromSource();
        } catch (err) {
            console.log('generic.js: ' + err);
        }
    };
})();

document.addEventListener('DOMContentLoaded', genericInit);
