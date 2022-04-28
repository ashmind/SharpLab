import { useContext } from 'react';
import { optionContexts, OptionName, OptionTypeMap } from './contexts/optionContexts';

export const useOption = <TOptionName extends OptionName>(name: TOptionName): OptionTypeMap[TOptionName] =>
    useContext(optionContexts[name])[0];

export const useAndSetOption = <TOptionName extends OptionName>(name: TOptionName) =>
    useContext(optionContexts[name]);